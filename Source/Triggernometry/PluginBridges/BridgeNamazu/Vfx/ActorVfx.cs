using System;
using System.Collections.Generic;

namespace Triggernometry.PluginBridges.BridgeNamazu.Vfx;

public class ActorVfx : VfxBase {
	/// <summary> 注意 lock </summary>
	public static IReadOnlyDictionary<IntPtr, ActorVfx> Storage
		=> VfxManager.ActorVfxs;

	// public override bool TryRemove()
	//     => VfxManager.Remove(this);
}