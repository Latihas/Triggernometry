using System;

namespace Scarborough;

internal static class ThrowHelper {
	public static InvalidOperationException DeviceNotInitialized() => new("The DirectX device is not initialized");

	public static InvalidOperationException UseBeginScene() => new("Use BeginScene before drawing anything");
}