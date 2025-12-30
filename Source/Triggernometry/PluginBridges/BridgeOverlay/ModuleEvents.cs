using Newtonsoft.Json.Linq;
using System;
using RainbowMage.OverlayPlugin;
using Triggernometry.Core;
using Triggernometry.Localization;


namespace Triggernometry.PluginBridges
{
    [OverlayModule]
    public static class ModuleEvents
    {
        public static bool Ready;
        private static EventDispatcher _eventDispatcher;

        static ModuleEvents()
        {
            Initialize();
        }

        internal static void Initialize()
        {
            try
            {
                _eventDispatcher = BridgeOverlay.Container.Resolve<EventDispatcher>();
                Ready = true;
            }
            catch (Exception ex)
            {
                RealPlugin.Instance.UnfilteredAddToLog(RealPlugin.DebugLevelEnum.Error,
                    I18n.Translate("internal/BridgeOverlay/failInitModule",
                    "OverlayPlugin {1} module initialization failed due to: {0}",
                    ex.ToString(), "Combatant")
                );
                Ready = false;
                return;
            }
            Ready = true;
        }
        

        public static JToken CallOverlayHandler(JObject jObject)
        {
            return _eventDispatcher.CallHandler(jObject);
        }

    }
}
