using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Triggernometry.Expressions.String.Utils;
using TriggernometryProxy;

namespace Triggernometry.PluginBridges.BridgeNamazu.Modules;

public class UseActionModule : ModuleBase {
	private MouseToWorldDelegate MouseToWorldD;

	private unsafe delegate void MouseToWorldDelegate(ActionManager* a1, uint spellid, byte a3, byte* result);

	public UseActionModule() {
		ScanMethod = () => {
			MouseToWorldD = Marshal.GetDelegateForFunctionPointer<MouseToWorldDelegate>(ProxyPlugin.SigScanner.ScanText(
				"4C 8B DC 49 89 5B ?? 49 89 6B ?? 49 89 73 ?? 57 48 81 EC ?? ?? ?? ?? 33 C0"));
		};
	}

	[CallbackMethod("UseAction")]
	internal void CbUseAction(string command) {
		CheckBeforeExecution(command);
		var (actionType, actionId, targetId, mode)
			= command.ParseArgs<ActionType, uint, uint, ActionManager.UseActionMode>(
				(2, 0xE0000000),
				(3, ActionManager.UseActionMode.None)
			);
		UseAction(actionType, actionId, targetId, mode);
	}

	public unsafe bool UseAction(ActionType actionType, uint actionId, uint targetId, ActionManager.UseActionMode mode = ActionManager.UseActionMode.None) {
		CheckIfAnyZeroPtr();
		var extraParam = (uint)(actionType == ActionType.Item ? 0xFFFF : 0);
		const uint comboRouteID = 0;
		var result = RunOnFrameworkThread(() => ActionManager.Instance()->UseAction((ActionType)(int)actionType, actionId, targetId, extraParam, (ActionManager.UseActionMode)(int)mode, comboRouteID, (bool*)0));
		if (result)
			NamazuLog($"[UseAction] {actionType} ({(int)actionType}), action = {actionId} (0x{actionId:X}), target = {targetId:X}, mode = {mode} ({(int)mode})");
		return result;
	}

	[CallbackMethod("UseActionLocation")]
	internal void CbUseActionLocation(string command) {
		CheckBeforeExecution(command);
		var (actionType, actionId, x, y, z, extraParam)
			= command.ParseArgs<ActionType, uint, float, float, float, uint>(
				(1, 0xE0000000),
				(2, 0), (3, 0), (4, 0),
				(5, 0)
			);
		RunOnFrameworkThreadV(() => UseActionLocation(actionType, actionId, x, y, z, extraParam));
	}

	public unsafe bool UseActionLocation(ActionType actionType, uint actionId, float x, float y, float z, uint extraParam = 0) {
		CheckIfAnyZeroPtr();
		const uint targetId = DataStringHelper.HexOrDecId.Default;
		var posPtr = new Vector3(x, z, y);
		var result = ActionManager.Instance()->UseActionLocation((ActionType)(int)actionType, actionId, targetId, &posPtr, extraParam);
		if (result) {
			NamazuLog($"[UseActionLocation]: {actionType} ({(byte)actionType}); action = {actionId} (0x{actionId:X}) @ ({x:0.##}, {y:0.##}, {z:0.##})");
		}
		return result;
	}

	//https://github.com/44451516/SmartCast/blob/master/SmartCast.cs
	private unsafe void MouseToWorld(out bool mouseOnWorld, out bool success, out Vector3 worldPos, uint actionId = 0xFFFFFFFF, byte actionType = (byte)ActionType.FieldMarker) {
		CheckIfAnyZeroPtr();
		var resultPtr = stackalloc byte[0x20];
		MouseToWorldD(ActionManager.Instance(), actionId, actionType, resultPtr);
		mouseOnWorld = resultPtr[0] == 1;
		success = resultPtr[1] == 1;
		worldPos = *(Vector3*)(resultPtr + 0x10);
	}

	// 似乎是借用尝试放置标点时调用的函数获取鼠标位置
	[ScriptingMethod("MouseToWorld")]
	public Vector3? MouseToWorld() {
		MouseToWorld(out var mouseOnWorld, out var success, out var worldPos);
		if (success && mouseOnWorld) return worldPos;
		return null;
	}

	[ScriptingMethod("IsMouseInSight")]
	public bool IsMouseInSight() {
		MouseToWorld(out _, out var success, out _);
		return success;
	}
}