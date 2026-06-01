using System;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage;
using Triggernometry.Core;
using Triggernometry.Localization;

namespace Triggernometry.PluginBridges;

[OverlayModule]
internal static class ModuleAtkStageMemory {
	public static bool Ready;
	public static readonly IAtkStageMemory AtkStageMemoryManager;

	static ModuleAtkStageMemory() {
		try {
			AtkStageMemoryManager = BridgeOverlay.Container.Resolve<IAtkStageMemory>();
			Ready = true;
		} catch (Exception ex) {
			RealPlugin.Instance.UnfilteredAddToLog(RealPlugin.DebugLevelEnum.Error,
				I18n.Translate("internal/BridgeOverlay/initfail", "OverlayPlugin initialization failed due to: {0}", ex.ToString())
			);
			Ready = false;
		}
	}

	#region AtkStageMemory

	public static IntPtr GetAddonAddress(string name) => AtkStageMemoryManager.GetAddonAddress(name);

	public static object GetAddon(string name) => AtkStageMemoryManager.GetAddon(name);

	#endregion AtkStageMemory
}