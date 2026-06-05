using System;
using RainbowMage.OverlayPlugin;
using Triggernometry.Core;
using TriggernometryProxy;

namespace Triggernometry.PluginBridges;

public static class BridgeOverlay {
	public static bool Ready;
	public static TinyIoCContainer Container;

	static BridgeOverlay() {
		Initialize();
	}

	public static void Initialize() {
		PluginMain op = ProxyPlugin.DalamudPlugin.OverlayPlugin;
		if (op == null) {
			RealPlugin.Instance.UnfilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, "OverlayPlugin not found");
			Ready = false;
			return;
		}
		Container = op._container;
		Ready = true;
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

[AttributeUsage(AttributeTargets.Class)]
public class OverlayModuleAttribute : Attribute;