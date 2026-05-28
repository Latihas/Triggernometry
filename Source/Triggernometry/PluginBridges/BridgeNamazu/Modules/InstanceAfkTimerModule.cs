using System;
using Dalamud;
using Triggernometry.Expressions.String.Utils;

namespace Triggernometry.PluginBridges.BridgeNamazu.Modules;

public class InstanceAfkTimerModule : ModuleBase {
	public IntPtr PatchPtr;
	// F3 0F11 53 1C         - movss [rbx+1C],xmm2  副本外计时器
	// F3 0F11 43 14         - movss [rbx+14],xmm0  副本内计时器
	public byte[] PatchedBytes = [0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90];
	public byte[] OriginalBytes = [0xF3, 0x0F, 0x11, 0x53, 0x1C, 0xF3, 0x0F, 0x11, 0x43, 0x14];

	public InstanceAfkTimerModule() {
		ScanMethod = () => {
			// E9 在此仅用于定位 不跳转！
			PatchPtr = Scanner.ScanText("E9 ? ? ? ? F3 0F 10 53 1C", nameof(PatchPtr)) + 0x23;
		};
	}

	[CallbackMethod("DisableInstanceAfkTimer")]
	internal void CbDisableInstanceAfkTimer(string cmd) {
		CheckBeforeExecution(cmd);
		if (GetConfig<bool>("InstanceAfkTimer") == false) return; // ignored
		var shouldDisable = cmd.ParseDataOrDefault(true);
		DisableInstanceTimer(shouldDisable);
	}

	public void DisableInstanceTimer(bool shouldDisable) {
		CheckIfAnyZeroPtr();
		RunOnFrameworkThreadV(() => {
			SafeMemory.ReadBytes(PatchPtr, OriginalBytes.Length, out var currentBytes);
			bool? isDisabled = currentBytes.SequenceEqual(PatchedBytes) ? true :
				currentBytes.SequenceEqual(OriginalBytes) ? false : null;
			if (isDisabled == null) {
				WarningLog("[鲶鱼精邮差扩展] 当前副本计时疑似被其他插件修改，无法禁用副本计时器。");
				return;
			}
			SafeMemory.WriteBytes(PatchPtr, shouldDisable ? PatchedBytes : OriginalBytes);
			if (isDisabled == shouldDisable) {
				CustomLog(shouldDisable ? "[鲶鱼精邮差扩展] 已禁用副本计时器。" : "[鲶鱼精邮差扩展] 已恢复副本计时器。");
			}
		});
	}
}