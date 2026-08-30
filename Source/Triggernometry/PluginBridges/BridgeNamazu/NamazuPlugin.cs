using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using PostNamazu;
using PostNamazu.Actions;
using Triggernometry.FFXIV;

namespace Triggernometry.PluginBridges.BridgeNamazu;

/// <summary>
///     Wrapper for PostNamazu.PostNamazu
/// </summary>
public class NamazuPlugin(PostNamazu.PostNamazu plugin) {
	private readonly PostNamazu.PostNamazu _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
	public NamazuModule CommandModule => GetOriginalModuleByName("Command");
	public NamazuModule MarkModule => GetOriginalModuleByName("Mark");
	public NamazuModule NormalCommandModule => GetOriginalModuleByName("NormalCommand");
	public NamazuModule PresetModule => GetOriginalModuleByName("Preset");
	public NamazuModule QueueModule => GetOriginalModuleByName("Queue");
	public NamazuModule SendKeyModule => GetOriginalModuleByName("SendKey");
	public NamazuModule WayMarkModule => GetOriginalModuleByName("WayMark");

	private GreyMagicExternalProcessMemory _Memory;
	public GreyMagicExternalProcessMemory Memory => _Memory ??= new GreyMagicExternalProcessMemory();

	private NamazuScanner _SigScanner;
	public NamazuScanner SigScanner {
		get {
			var current = _plugin.SigScanner;
			if (_SigScanner?.RawScanner != current) {
				_SigScanner = current == null ? null : new NamazuScanner(current);
			}
			return _SigScanner;
		}
	}
	public bool IsReady => true;

	public PostNamazuUi PluginUI => _plugin.PluginUi;

	public NamazuModule GetOriginalModuleByName(string moduleName) =>
		_plugin.Modules.FirstOrDefault(m => m.GetType().Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase));

	public Dictionary<string, bool> ActionEnabled => _plugin.ActionEnabled;

	public bool IsActionEnabled(string cmdOrModuleName) {
		var moduleName = _commandToModuleNames.GetValueOrDefault(cmdOrModuleName, cmdOrModuleName);
		if (!ActionEnabled.TryGetValue(moduleName, out var enabled)) {
			throw new KeyNotFoundException($"Module '{moduleName}' not found.");
		}
		return enabled;
	}

	public void DoAction(string command, string payload)
		=> _plugin.DoAction(command, payload);

	/// <summary>
	///     Force an action to be executed, bypassing the user config checks.
	/// </summary>
	public void DoActionForce(string command, string payload, string moduleName = null) {
		if (moduleName == null && !_commandToModuleNames.TryGetValue(command, out moduleName)) {
			throw new ArgumentException($"Command '{command}' does not map to a module name.", nameof(command));
		}
		ExecuteWithForcedModuleState(moduleName, () => DoAction(command, payload));
	}

	public void ExecuteWithForcedModuleState(string moduleName, Action visitor) {
		if (!ActionEnabled.TryGetValue(moduleName, out var enabled)) {
			throw new KeyNotFoundException($"Module '{moduleName}' not found.");
		}
		try {
			ActionEnabled[moduleName] = true;
			visitor();
		} finally {
			ActionEnabled[moduleName] = enabled;
		}
	}

	public T ExecuteWithForcedModuleState<T>(string moduleName, Func<T> visitor) {
		if (!ActionEnabled.TryGetValue(moduleName, out var enabled)) {
			throw new KeyNotFoundException($"Module '{moduleName}' not found.");
		}
		try {
			ActionEnabled[moduleName] = true;
			return visitor();
		} finally {
			ActionEnabled[moduleName] = enabled;
		}
	}

	private static readonly IReadOnlyDictionary<string, string> _commandToModuleNames
		= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
			["command"] = "Command",
			["DoTextCommand"] = "Command",
			["mark"] = "Mark",
			["normalcommand"] = "NormalCommand",
			["DoNormalTextCommand"] = "NormalCommand",
			["preset"] = "Preset",
			["DoInsertPreset"] = "Preset",
			["queue"] = "Queue",
			["DoQueueActions"] = "Queue",
			["sendkey"] = "DoSendKey",
			["place"] = "WayMark",
			["DoWaymarks"] = "WayMark"
		};

	// Region detection
	public bool IsCN => _plugin.IsCN;
	public bool IsTC => GameLanguage.Language == GameLanguageEnum.TCN;
	// public IntPtr FrameworkPtr => _plugin.FrameworkPtr;

	// public void ExecuteInFrameLock(Action action) {
	// 	_plugin.ExecuteInFrameLock(action);
	// }
	//
	// public T ExecuteInFrameLock<T>(Func<T> func) {
	// 	return _plugin.ExecuteInFrameLock<T>(func);
	// }
	//
	private static class NativeDelegateBuilder {
		private sealed class Signature(Type returnType, Type[] paramTypes) : IEquatable<Signature> {
			private readonly Type ReturnType = returnType;
			private readonly Type[] ParamTypes = paramTypes;

			public bool Equals(Signature other) {
				if (other == null) return false;
				if (ReturnType != other.ReturnType || ParamTypes.Length != other.ParamTypes.Length)
					return false;
				return !ParamTypes.Where((t, i) => t != other.ParamTypes[i]).Any();
			}

			public override bool Equals(object obj) => Equals(obj as Signature);

			public override int GetHashCode() {
				unchecked {
					return ParamTypes.Aggregate(ReturnType.GetHashCode(), (current, t) => current * 397 ^ t.GetHashCode());
				}
			}
		}

		private static readonly Dictionary<Signature, Type> Cache = new();
		private static readonly ModuleBuilder Module = CreateModule();
		private static int Counter;

		private static ModuleBuilder CreateModule() {
			var asm = AssemblyBuilder.DefineDynamicAssembly(
				new AssemblyName("Triggernometry.BridgeNamazu.NativeDelegates"),
				AssemblyBuilderAccess.Run);
			return asm.DefineDynamicModule("Main");
		}

		private static Type GetOrCreate(CallingConvention convention, Type returnType, Type[] paramTypes) {
			var key = new Signature(returnType, paramTypes);
			lock (Cache) {
				if (Cache.TryGetValue(key, out var cached)) return cached;
				var tb = Module.DefineType(
					"NativeDelegate_" + Interlocked.Increment(ref Counter),
					TypeAttributes.Public | TypeAttributes.Sealed,
					typeof(MulticastDelegate));
				var ufpCtor = typeof(UnmanagedFunctionPointerAttribute)
					.GetConstructor([typeof(CallingConvention)]);
				tb.SetCustomAttribute(new CustomAttributeBuilder(ufpCtor, [convention]));
				var ctor = tb.DefineConstructor(
					MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName,
					CallingConventions.Standard,
					[typeof(object), typeof(IntPtr)]);
				ctor.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
				var invoke = tb.DefineMethod("Invoke",
					MethodAttributes.Public | MethodAttributes.HideBySig
					                        | MethodAttributes.NewSlot | MethodAttributes.Virtual,
					returnType, paramTypes);
				invoke.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
				var type = tb.CreateTypeInfo().AsType();
				Cache[key] = type;
				return type;
			}
		}

		public static object Invoke(IntPtr funcPtr, CallingConvention convention, Type returnType, object[] args) {
			var paramTypes = new Type[args.Length];
			for (var i = 0; i < args.Length; i++)
				paramTypes[i] = args[i]?.GetType() ?? typeof(object);
			var delType = GetOrCreate(convention, returnType, paramTypes);
			var d = Marshal.GetDelegateForFunctionPointer(funcPtr, delType);
			return d.DynamicInvoke(args);
		}
	}

	public void Call(IntPtr ptr, params object[] args)
		=> NativeDelegateBuilder.Invoke(ptr, CallingConvention.Winapi, typeof(void), args);

	public T Call<T>(IntPtr ptr, params object[] args) where T : struct
		=> (T)NativeDelegateBuilder.Invoke(ptr, CallingConvention.Winapi, typeof(T), args);

	public void DirectCall(IntPtr ptr, params object[] args)
		=> NativeDelegateBuilder.Invoke(ptr, CallingConvention.Winapi, typeof(void), args);

	public T DirectCall<T>(IntPtr ptr, params object[] args) where T : struct
		=> (T)NativeDelegateBuilder.Invoke(ptr, CallingConvention.Winapi, typeof(T), args);

	public void CallVirtualFunction(IntPtr objAddress, int vFuncIndex, params object[] args)
		=> CallVirtualFunction<IntPtr>(objAddress, vFuncIndex, args);

	public T CallVirtualFunction<T>(IntPtr objAddress, int vFuncIndex, params object[] args) where T : struct {
		var vTablePtr = Memory.Read<IntPtr>(objAddress);
		var vFuncPtr = Memory.Read<IntPtr>(vTablePtr + IntPtr.Size * vFuncIndex);
		return Call<T>(vFuncPtr, [objAddress, .. args]);
	}
}