using System;
using System.Collections.Generic;
using Triggernometry.PluginBridges.BridgeNamazu.Modules;

namespace Triggernometry.PluginBridges.BridgeNamazu.Vfx;

public class ActorVfx : Vfx {
	/// <summary> 注意 lock </summary>
        public static IReadOnlyDictionary<IntPtr, ActorVfx> Storage 
            => VfxManager.ActorVfxs;

        public override bool TryRemove()
            => VfxManager.Remove(this);

}