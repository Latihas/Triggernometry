using System;
using RainbowMage.OverlayPlugin;
using Triggernometry.Core;
using Triggernometry.Localization;

namespace Triggernometry.PluginBridges;

public static class BridgeOverlay
{
    public const string PluginName = "OverlayPlugin.dll";
    public const string PluginType = "RainbowMage.OverlayPlugin.PluginLoader";

    public static bool Ready;
    public static PluginMain OverlayPlugin;
    public static TinyIoCContainer Container;

    static BridgeOverlay()
    {
        Initialize();
    }

    public static void Initialize()
    {
        PluginMain op =OverlayPlugin=ProxyPlugin.DalamudPlugin.OverlayPlugin;
        if (op == null)
        { 
            RealPlugin.Instance.UnfilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, "OverlayPlugin not found");
            Ready = false;
            return;
        }

        // get the container and resolve method
        try
        {
            Container = op._container;
            Ready = true;
        }
        catch (Exception ex)
        {
            RealPlugin.Instance.UnfilteredAddToLog(RealPlugin.DebugLevelEnum.Error, 
                                                   I18n.Translate("internal/BridgeOverlay/failInit", 
                                                                  "OverlayPlugin-related initialization failed due to: {0}", ex.ToString())
            );
            Ready = false;
        }
    }

    // public static object Resolve(this TinyIoCContainer container, string typeName)
    // {
        // Type type = Type.GetType(typeName)
        //             ?? throw new ReflectionNotFoundException($"{typeName} type");
        // MethodInfo resolveMethodSpecific = _resolveMethodGeneric.MakeGenericMethod(type);
        // object resolvedInstance = resolveMethodSpecific.Invoke(container, null) 
        //                           ?? throw new ReflectionNotFoundException($"{typeName} instance");
        // return container.Resolve(typeName);
    // }

}

public class ReflectionNotFoundException : Exception
{
    public ReflectionNotFoundException(string objectName) : base(I18n.Translate(
                                                                     "internal/BridgeOverlay/reflectionNotFound",
                                                                     "Failed to find reflection object ({0}) during initializing OverlayPlugin-related modules.", 
                                                                     objectName))
    {
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class OverlayModuleAttribute : Attribute { }
