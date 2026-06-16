using System;
using RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;
using Triggernometry.Core;
using Triggernometry.Localization;

namespace Triggernometry.PluginBridges;

[OverlayModule]
internal static class ModuleInCombat {
	public static readonly bool Ready;
	private static readonly IInCombatMemory _inCombatMemoryManager;

	static ModuleInCombat() {
		try {
			_inCombatMemoryManager = BridgeOverlay.Container.Resolve<IInCombatMemory>();
			Ready = true;
		} catch (Exception ex) {
			RealPlugin.Instance.UnfilteredAddToLog(RealPlugin.DebugLevelEnum.Error,
				I18n.Translate("internal/BridgeOverlay/failInitModule",
					"OverlayPlugin {1} module initialization failed due to: {0}",
					ex.ToString(), "InCombat")
			);
			Ready = false;
		}
	}

	public static bool GetInCombat() {
		if (!Ready) return false;
		return _inCombatMemoryManager.GetInCombat();
	}
}