using System;
using System.Collections.Generic;
using System.Linq;
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
	// public void Call(IntPtr ptr, params object[] args) {
	// 	_plugin.Call(ptr, args);
	// }
	//
	// public T Call<T>(IntPtr ptr, params object[] args) where T : struct {
	// 	return _plugin.Call<T>(ptr, args);
	// }
	//
	// public void DirectCall(IntPtr ptr, params object[] args) {
	// 	_plugin.DirectCall(ptr, args);
	// }
	//
	// public T DirectCall<T>(IntPtr ptr, params object[] args) where T : struct {
	// 	return _plugin.DirectCall<T>(ptr, args);
	// }
	//
	// public void CallVirtualFunction(IntPtr objAddress, int vFuncIndex, params object[] args)
	// 	=> CallVirtualFunction<IntPtr>(objAddress, vFuncIndex, args);
	//
	// public T CallVirtualFunction<T>(IntPtr objAddress, int vFuncIndex, params object[] args) where T : struct {
	// 	var vTablePtr = Memory.Read<IntPtr>(objAddress);
	// 	var vFuncPtr = Memory.Read<IntPtr>(vTablePtr + 8 * vFuncIndex);
	// 	return Call<T>(vFuncPtr, new object[] { objAddress }.Concat(args).ToArray());
	// }
}