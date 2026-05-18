using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.String;
using Triggernometry.Core;
using Triggernometry.Expressions.String.Utils;
using Triggernometry.PluginBridges.BridgeNamazu.Vfx;
using TriggernometryProxy;
using static Triggernometry.PScript.ScriptUtils;

namespace Triggernometry.PluginBridges.BridgeNamazu.Modules;

public class VfxModule : ModuleBase {
	internal static readonly Dictionary<IntPtr, ActorVfx> _actorVfxs = new();
	internal static readonly Dictionary<IntPtr, StaticVfx> _staticVfxs = new();

	public static IReadOnlyDictionary<IntPtr, ActorVfx> ActorVfxs {
		get {
			lock (_actorVfxs)
				return new Dictionary<IntPtr, ActorVfx>(_actorVfxs);
		}
	}
	
	public static IReadOnlyDictionary<IntPtr, StaticVfx> StaticVfxs {
		get {
			lock (_staticVfxs)
				return new Dictionary<IntPtr, StaticVfx>(_staticVfxs);
		}
	}

	public static void ClearVfxCache() {
		lock (_actorVfxs) _actorVfxs.Clear();
		lock (_staticVfxs) _staticVfxs.Clear();
		// if (RealPlugin.Instance.cfg.UseImGui4VfxModule) ClearAllIGShape();
	}

	public unsafe VfxModule() {
		ScanMethod = () => {
			ClearVfxCache();
			var StaticVfxCreateSig = VfxObject.Addresses.Create.String;
			const string StaticVfxRunSig = "E8 ?? ?? ?? ?? B0 02 EB 02";
			const string StaticVfxRemoveSig = "40 53 48 83 EC 20 48 8B D9 48 8B 89 ?? ?? ?? ?? 48 85 C9 74 28 33 D2 E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9";
			const string ActorVfxCreateSig = "40 53 55 56 57 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 AC 24 ?? ?? ?? ?? 0F 28 F3 49 8B F8";
			const string ActorVfxRemoveSig = "0F 11 48 10 48 8D 05"; // the weird one

			var staticVfxCreateAddress = ProxyPlugin.SigScanner.ScanText(StaticVfxCreateSig);
			var staticVfxRemoveAddress = ProxyPlugin.SigScanner.ScanText(StaticVfxRemoveSig);
			var actorVfxCreateAddress = ProxyPlugin.SigScanner.ScanText(ActorVfxCreateSig);
			var actorVfxRemoveAddresTemp = ProxyPlugin.SigScanner.ScanText(ActorVfxRemoveSig) + 7;
			var actorVfxRemoveAddress = Marshal.ReadIntPtr(actorVfxRemoveAddresTemp + Marshal.ReadInt32(actorVfxRemoveAddresTemp) + 4);

			ActorVfxCreateD ??= Marshal.GetDelegateForFunctionPointer<ActorVfxCreateDelegate>(actorVfxCreateAddress);
			ActorVfxRemoveD ??= Marshal.GetDelegateForFunctionPointer<ActorVfxRemoveDelegate>(actorVfxRemoveAddress);
			StaticVfxRemoveD ??= Marshal.GetDelegateForFunctionPointer<StaticVfxRemoveDelegate>(staticVfxRemoveAddress);
			StaticVfxRunD = Marshal.GetDelegateForFunctionPointer<StaticVfxRunDelegate>(ProxyPlugin.SigScanner.ScanText(StaticVfxRunSig));
			StaticVfxCreateD = Marshal.GetDelegateForFunctionPointer<VfxObject.Delegates.Create>(staticVfxCreateAddress);

			ProxyPlugin.StaticVfxRemoveHook ??= ProxyPlugin.GameInteropProvider.HookFromAddress<StaticVfxRemoveDelegate>(staticVfxRemoveAddress, StaticVfxRemoveDetour);
			ProxyPlugin.ActorVfxRemoveHook ??= ProxyPlugin.GameInteropProvider.HookFromAddress<ActorVfxRemoveDelegate>(actorVfxRemoveAddress, ActorVfxRemoveDetour);
			ProxyPlugin.StaticVfxRemoveHook.Enable();
			ProxyPlugin.ActorVfxRemoveHook.Enable();
		};
	}

	public static unsafe VfxObject* StaticVfxRemoveDetour(VfxObject* vfxPtr) {
		if (RealPlugin.Instance.cfg.PostnamazuModuleDisabled.Contains(nameof(VfxModule))) return ProxyPlugin.StaticVfxRemoveHook.Original(vfxPtr);
		Task.Run(() => {
			lock (_staticVfxs) {
				if (_staticVfxs.TryGetValue((IntPtr)vfxPtr, out var vfx)) {
					try {
						vfx.Removed = true;
					} finally {
						_staticVfxs.Remove((IntPtr)vfxPtr);
					}
				}
			}
		});
		return ProxyPlugin.StaticVfxRemoveHook.Original(vfxPtr);
	}

	public static unsafe VfxObject* ActorVfxRemoveDetour(VfxObject*  vfxPtr, char a2) {
		if (RealPlugin.Instance.cfg.PostnamazuModuleDisabled.Contains(nameof(VfxModule))) return ProxyPlugin.ActorVfxRemoveHook.Original(vfxPtr, a2);
		Task.Run(() => {
			lock (_actorVfxs) {
				if (_actorVfxs.TryGetValue((IntPtr)vfxPtr, out var vfx)) {
					try {
						vfx.Removed = true;
					} finally {
						_actorVfxs.Remove((IntPtr)vfxPtr);
					}
				}
			}
		});
		return ProxyPlugin.ActorVfxRemoveHook.Original(vfxPtr, a2);
	}
	

	public static VfxObject.Delegates.Create StaticVfxCreateD;

	public unsafe delegate VfxObject* StaticVfxRunDelegate(VfxObject* vfx, float a1, int a2);

	public static StaticVfxRunDelegate StaticVfxRunD;

	public unsafe delegate VfxObject* ActorVfxRemoveDelegate(VfxObject*  vfx, char a2);

	public ActorVfxRemoveDelegate ActorVfxRemoveD;

	public unsafe delegate VfxObject* ActorVfxCreateDelegate(string path, IntPtr a2, IntPtr a3, float a4, char a5, ushort a6, char a7);

	public ActorVfxCreateDelegate ActorVfxCreateD;

	public unsafe delegate VfxObject* StaticVfxRemoveDelegate(VfxObject* vfx);

	public StaticVfxRemoveDelegate StaticVfxRemoveD;

	#region ActorVfx

	/// <summary> 点名特效 </summary>
	[CallbackMethod("LockOn")]
	internal unsafe void CbLockOn(string cmd) {
		CheckBeforeExecution(cmd);
		if (GetConfig<bool>("ActorVfx") == false) return; // ignored
		var (tgtAddress, vfxName, duration) = cmd.ParseArgs<IntPtr, string, double>((2, -1.0)); // 默认不移除
		CheckIfVfxNameTooShort(vfxName, "LockOn");
		var vfx = GreyMagicMemoryBase.ExecuteWithLock(() => LockOnCreate(tgtAddress, vfxName));
		if (vfx == null) return;
		var vfxPtr = vfx.Ptr;
		ScheduleActorVfxRemove(vfxPtr, duration, true);
	}

	/// <summary> 连线特效 </summary>
	[CallbackMethod("Channeling")]
	internal unsafe void CbChanneling(string cmd) {
		CheckBeforeExecution(cmd);
		if (GetConfig<bool>("ActorVfx") == false) return; // ignored
		var (srcAddress, tgtAddress, vfxName, duration) = cmd.ParseArgs<IntPtr, IntPtr, string, double>((3, 3.0)); // 默认持续时间 3 秒
		CheckIfVfxNameTooShort(vfxName, "Channeling");
		var vfx = GreyMagicMemoryBase.ExecuteWithLock(() => ChannelingCreate(srcAddress, tgtAddress, vfxName));
		if (vfx == null) return;
		var vfxPtr = vfx.Ptr;
		ScheduleActorVfxRemove(vfxPtr, duration);
	}

	/// <summary> 咏唱特效 </summary>
	[CallbackMethod("CastVfx")]
	internal unsafe void CbCastVfx(string cmd) {
		CheckBeforeExecution(cmd);
		if (GetConfig<bool>("ActorVfx") == false) return; // ignored
		var (srcAddress, vfxName, duration) = cmd.ParseArgs<IntPtr, string, double>((2, 3.0)); // 默认持续时间 3 秒
		CheckIfVfxNameTooShort(vfxName, "CastVfx");
		var vfx = GreyMagicMemoryBase.ExecuteWithLock(() => CastVfxCreate(srcAddress, vfxName));
		if (vfx == null) return;
		var vfxPtr =  vfx.Ptr;
		ScheduleActorVfxRemove(vfxPtr, duration);
	}

	/// <summary> 通用 ActorVfx </summary>
	[CallbackMethod("ActorVfx")]
	internal unsafe void CbActorVfx(string cmd) {
		CheckBeforeExecution(cmd);
		if (GetConfig<bool>("ActorVfx") == false) return; // ignored
		var (srcAddress, tgtAddress, vfxName, duration) = cmd.ParseArgs<IntPtr, IntPtr, string, double>((3, 3.0)); // 默认持续时间 3 秒
		CheckIfVfxNameTooShort(vfxName, "ActorVfx");
		var vfx = GreyMagicMemoryBase.ExecuteWithLock(() => ActorVfxCreate(srcAddress, tgtAddress, vfxName));
		if (vfx == null) return;
		var vfxPtr = vfx.Ptr;
		ScheduleActorVfxRemove(vfxPtr, duration);
	}

	public ActorVfx LockOnCreate(IntPtr tgtAddress, string vfxName, string tag = Vfx.Vfx.DefaultTag)
		=> ActorVfxCreate(tgtAddress, tgtAddress, $"vfx/lockon/eff/{vfxName}.avfx", tag, true);

	public ActorVfx ChannelingCreate(IntPtr srcAddress, IntPtr tgtAddress, string vfxName, string tag = Vfx.Vfx.DefaultTag)
		=> ActorVfxCreate(srcAddress, tgtAddress, $"vfx/channeling/eff/{vfxName}.avfx", tag);

	public ActorVfx CastVfxCreate(IntPtr srcAddress, string vfxName, string tag = Vfx.Vfx.DefaultTag)
		=> ActorVfxCreate(srcAddress, srcAddress, $"vfx/common/eff/{vfxName}.avfx", tag);

	public unsafe ActorVfx ActorVfxCreate(IntPtr srcAddress, IntPtr tgtAddress, string fullPath, string tag = Vfx.Vfx.DefaultTag, bool scheduleRemovalByGame = false, float unknownParamTest = -1f) {
		return GreyMagicMemoryBase.ExecuteWithLock(() => {
			CheckIfAnyZeroPtr();
			if (!scheduleRemovalByGame) CheckIfAnyZeroPtr();
			CheckIfVfxPathValid(fullPath);
			if ((long)srcAddress <= 0xFFFF || (long)tgtAddress <= 0xFFFF)
				throw new Exception($"[鲶鱼精邮差扩展] ActorVfxCreate ({fullPath}) 实体地址无效：src = {(long)srcAddress:X}, tgt = {(long)tgtAddress:X}");
			ActorVfx vfx = null;
			// if (RealPlugin.Instance.cfg.UseImGui4VfxModule) {
			// 	// if(ImGuiReplaceDict.TryGetValue(fullPath,out var type))
			// 	// vfx = new ActorVfx {
			// 	//     Ptr = 0,
			// 	//     Path = fullPath,
			// 	//     Tag = tag
			// 	// imguiobj
			// 	// };
			// 	// Triggernometry.PScript.ScriptUtils.ScriptDrawList.Add(new IGRect(Me_Position, GetGameObjectById_Position(s)
			// 	//TODO Collect data
			// 	RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, $"ActorVfx not in dict: {fullPath}({tag})");
			// }
			if (vfx == null) {
				var vfxPtr = ActorVfxCreateD(fullPath, srcAddress, tgtAddress, unknownParamTest, (char)0, 0, (char)0);
				vfx = new ActorVfx {
					Ptr = vfxPtr,
					Path = fullPath,
					Tag = tag
				};
				if (!scheduleRemovalByGame) // 临时应对方式，暂时未能检测 LockOn 是否已经被移除，所以不主动注册
				{
					lock (_actorVfxs) {
						_actorVfxs[(IntPtr)vfxPtr] = vfx;
					}
				}
				Custom2Log($"[ActorVfxCreate] {fullPath} @ {(long)vfxPtr:X}");
			}
			return vfx;
		});
	}

	public unsafe bool TryActorVfxRemove(VfxObject*  vfxPtr, bool scheduleRemovalByGame = false) // 待优化：判断是否存在 vfx
	{
		return GreyMagicMemoryBase.ExecuteWithLock(() => {
			CheckIfAnyZeroPtr();
			if (!ProxyPlugin.ActorVfxRemoveHook.IsEnabled) {
				RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"ActorVfxRemoveHook未就绪，不移除特效 {(IntPtr)vfxPtr:X}"); //TODO
				return false;
			}
			try {
				lock (_actorVfxs) {
					if (!scheduleRemovalByGame) return false;
					if (!_actorVfxs.TryGetValue((IntPtr)vfxPtr, out var vfx)) {
						Custom2Log($"[ActorVfx] 移除特效：（已移除）@{(IntPtr)vfxPtr:X}");
						return false;
					}
					vfx.Removed = true;
				}
				ActorVfxRemoveD(vfxPtr, (char)1);
			} finally {
				lock (_actorVfxs) _actorVfxs.Remove((IntPtr)vfxPtr);
				Custom2Log($"[ActorVfx] 移除特效： @ {(IntPtr)vfxPtr:X}");
			}
			return true;
		});
	}


	public unsafe void ScheduleActorVfxRemove(VfxObject* vfxPtr, double duration, bool scheduleRemovalByGame = false) {
		if (duration >= 0 && (IntPtr)vfxPtr != IntPtr.Zero) {
			Task.Delay((int)(duration * 1000)).ContinueWith(_ => GreyMagicMemoryBase.ExecuteWithLock(() => TryActorVfxRemove(vfxPtr, scheduleRemovalByGame)));
		}
	}

	#endregion ActorVfx

	[CallbackMethod("Omen")]
	internal void CbOmen(string command)
		=> ProcessStaticVfx(command, "vfx/omen/eff/{0}.avfx");

	[CallbackMethod("StaticVfx")]
	internal void CbStaticVfx(string command)
		=> ProcessStaticVfx(command);

	private void ProcessStaticVfx(string rawArgs, string nameFormatTemplate = null) {
		GreyMagicMemoryBase.ExecuteWithLock(() => {
			CheckBeforeExecution(rawArgs);
			if (GetConfig<bool>("StaticVfx") == false) return; // ignored
			var (vfxName, t, x, y, z, h, scaleX, rawScaleY, rawScaleZ, r, g, b, a)
				= rawArgs.ParseArgs<string, float, float, float, float, float, float, float?, float?, float, float, float, float>(
					(6, 1), (7, null), (8, null), (9, 1), (10, 1), (11, 1), (12, 1));

			CheckIfVfxNameTooShort(vfxName, "StaticVfx");
			var vfxPath = nameFormatTemplate == null ? vfxName : string.Format(nameFormatTemplate, vfxName);
			var pos = new Vector3(x, y, z);
			var scales = new Vector3(scaleX, rawScaleY ?? scaleX, rawScaleZ ?? scaleX);
			var color = new Vector4(r, g, b, a);

			var vfx = StaticVfxCreate(vfxPath);
			vfx.Run();

			vfx.Pos = pos;
			vfx.Angle = h;
			if (scales != Vector3.One) vfx.Scales = scales;
			if (color != Vector4.One) vfx.Color = color;
			vfx.Update();

			vfx.ScheduleRemove(t);
		});
	}


	public unsafe StaticVfx StaticVfxCreate(string fullPath, string tag = Vfx.Vfx.DefaultTag) {
		return GreyMagicMemoryBase.ExecuteWithLock(() => {
			CheckIfAnyZeroPtr();
			CheckIfVfxPathValid(fullPath);
			const string pool = "Client.System.Scheduler.Instance.VfxObject";
			StaticVfx vfx = null;
			// if (RealPlugin.Instance.cfg.UseImGui4VfxModule) {
			// 	if (ImGuiReplaceDict.TryGetValue(fullPath, out var type))
			// 		vfx = new StaticVfx {
			// 			Ptr = (VfxObject*)0,
			// 			Path = fullPath,
			// 			Tag = tag
			// 		};
			// 	// Triggernometry.PScript.ScriptUtils.ScriptDrawList.Add(new IGRect(Me_Position, GetGameObjectById_Position(s)
			// 	//TODO Collect data
			// 	RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, $"StaticVfx not in dict: {fullPath}({tag})");
			// }
			if (vfx == null) {
				var vfxPtr = StaticVfxCreateD(new Utf8String(fullPath).StringPtr, new Utf8String(pool).StringPtr);
				vfx = new StaticVfx {
					Ptr = vfxPtr,
					Path = fullPath,
					Tag = tag
				};
				lock (_staticVfxs) {
					_staticVfxs[(IntPtr)vfxPtr] = vfx;
				}
				Custom2Log($"[StaticVfxCreate] {fullPath} @ {(long)vfxPtr:X}");
			}
			return vfx;
		});
	}

	public unsafe void StaticVfxRun(VfxObject* vfxPtr) {
		GreyMagicMemoryBase.ExecuteWithLock(() => {
			CheckIfAnyZeroPtr();
			StaticVfxRunD(vfxPtr, 0.0f, -1);
		});
	}

	// private void StaticVfxFadeout(IntPtr vfxPtr, float fadeFrames60)
	// {
	// 	CheckIfAnyZeroPtr(StaticVfxFadeOutPtr);
	// 	_ = Memory.CallInjected64<IntPtr>(StaticVfxFadeOutPtr, vfxPtr, fadeFrames60);
	// 	// _ = Memory.CallInjected64<IntPtr>(StaticVfxRemovePtr, vfxPtr);
	// }

	public unsafe bool TryStaticVfxRemove(VfxObject* vfxPtr) {
		return GreyMagicMemoryBase.ExecuteWithLock(() => {
			CheckIfAnyZeroPtr();
			if (!ProxyPlugin.StaticVfxRemoveHook.IsEnabled) {
				RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"StaticVfxRemoveHook未就绪，不移除特效 {(IntPtr)vfxPtr:X}");
				return false;
			}
			try {
				lock (_staticVfxs) {
					if (!_staticVfxs.TryGetValue((IntPtr)vfxPtr, out var vfx)) {
						Custom2Log($"[StaticVfx] 移除特效：（已移除）@{(IntPtr)vfxPtr:X}");
						return false;
					}
					vfx.Removed = true;
				}
				StaticVfxRemoveD(vfxPtr);
			} finally {
				lock (_staticVfxs) _staticVfxs.Remove((IntPtr)vfxPtr);
				Custom2Log($"[StaticVfx] 已移除特效记录 @ {(IntPtr)vfxPtr:X}");
			}
			return true;
		});
	}

	public unsafe void ScheduleStaticVfxRemove(VfxObject* vfxPtr, double duration) {
		if (duration >= 0 && (IntPtr)vfxPtr != IntPtr.Zero) {
			Task.Delay((int)(duration * 1000)).ContinueWith(_ => TryStaticVfxRemove(vfxPtr));
		}
	}


	private void CheckIfVfxNameTooShort(string vfxName, string methodName) {
		if (vfxName.Length <= 8)
			throw new Exception($"[鲶鱼精邮差扩展] {methodName} vfxName 参数过短：{vfxName}");
	}

	public static readonly Dictionary<string, ShapeType> ImGuiReplaceDict = new() {
		// {"m0071_fan180_01k2",ShapeType.Circle},
		// {"Rect",ShapeType.Rect},
	};

	private void CheckIfVfxPathValid(string vfxPath) {
		if (!vfxPath.EndsWith(".avfx", StringComparison.OrdinalIgnoreCase))
			throw new Exception($"[鲶鱼精邮差扩展] vfxName 不以 \".avfx\" 结尾：{vfxPath}");
		if (Regex.IsMatch(vfxPath, @"\.(?!avfx)", RegexOptions.IgnoreCase))
			throw new Exception($"[鲶鱼精邮差扩展] vfxName 包含错误的扩展名：{vfxPath}");
	}
}