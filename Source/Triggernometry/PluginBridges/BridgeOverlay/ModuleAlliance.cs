using System;
using RainbowMage.OverlayPlugin.MemoryProcessors.Party;
using Triggernometry.Core;
using Triggernometry.Localization;

namespace Triggernometry.PluginBridges;

[OverlayModule]
internal static class ModuleAlliance {
	public static readonly bool Ready;
	private static IPartyMemory _partyMemoryManager;

	static ModuleAlliance() {
		try {
			_partyMemoryManager = BridgeOverlay.Container.Resolve<IPartyMemory>();
			Ready = true;
		} catch (Exception ex) {
			RealPlugin.Instance.UnfilteredAddToLog(RealPlugin.DebugLevelEnum.Error,
				I18n.Translate("internal/BridgeOverlay/initfail", "OverlayPlugin initialization failed due to: {0}", ex.ToString())
			);
			Ready = false;
		}
	}

	#region PartyList

	public static object GetPartyLists() {
		if (!Ready) {
			RealPlugin.Instance.UnfilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, "OverlayPlugin not ready");
			return new object();
		}
		var o = _partyMemoryManager.GetPartyLists();
		return o;
	}

	public class PartyLists {
	}

	#endregion PartyList
}