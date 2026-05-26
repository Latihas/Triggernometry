using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dalamud.Plugin.Services;
using Triggernometry.PluginBridges.BridgeNamazu.Modules;
using TriggernometryProxy;

namespace Triggernometry.PluginBridges.BridgeNamazu.Vfx {
	internal static class VfxManager {
		public static readonly Dictionary<IntPtr, ActorVfx> ActorVfxs = [];

		public static readonly Dictionary<IntPtr, StaticVfx> StaticVfxs = [];

		// public static IReadOnlyDictionary<IntPtr, ActorVfx> ActorVfxs {
		// 	get {
		// 		lock (_actorVfxs) {
		// 			return new Dictionary<IntPtr, ActorVfx>(_actorVfxs);
		// 		}
		// 	}
		// }
		//
		// public static IReadOnlyDictionary<IntPtr, StaticVfx> StaticVfxs {
		// 	get {
		// 		lock (_staticVfxs) {
		// 			return new Dictionary<IntPtr, StaticVfx>(_staticVfxs);
		// 		}
		// 	}
		// }

		internal static VfxModule Module => BridgeNamazu.GetModule<VfxModule>();

		public static ActorVfx CreateActor(IntPtr srcAddress, IntPtr tgtAddress, string fullPath, string tag = null) {
			return Module.ActorVfxCreate(srcAddress, tgtAddress, fullPath, tag);
		}

		public static unsafe StaticVfx InitStatic(string fullPath, string? tag = null) {
			var vfxPtr = Module.StaticVfxCreate(fullPath);

			var vfx = new StaticVfx {
				Vfx = vfxPtr,
				Path = fullPath,
				Tag = tag ?? VfxBase.DefaultTag
			};

			Register(vfx);
			Module.StaticVfxRun(vfx.Vfx);

			return vfx;
		}

		public static unsafe bool Remove(VfxBase? vfx) {
			if (vfx == null || vfx.Vfx == null || (IntPtr)vfx.Vfx == IntPtr.Zero)
				return false;
			if (vfx is ActorVfx)
				return Module.TryActorVfxRemove(vfx.Vfx);
			if (vfx is StaticVfx)
				return Module.TryStaticVfxRemove(vfx.Vfx);
			return false;
		}

		public static void Clear() {
			lock (ActorVfxs) {
				ActorVfxs.Clear();
			}

			lock (StaticVfxs) {
				StaticVfxs.Clear();
			}
		}

		public static unsafe void Register(ActorVfx? vfx) {
			if (vfx == null || vfx.Vfx == null || (IntPtr)vfx.Vfx == IntPtr.Zero)
				return;

			lock (ActorVfxs) {
				ActorVfxs[(IntPtr)vfx.Vfx] = vfx;
			}
		}

		public static unsafe void Register(StaticVfx? vfx) {
			if (vfx == null || vfx.Vfx == null || (IntPtr)vfx.Vfx == IntPtr.Zero)
				return;

			lock (StaticVfxs) {
				StaticVfxs[(IntPtr)vfx.Vfx] = vfx;
			}
		}

		public static bool TryUnregisterActor(IntPtr ptr, out ActorVfx vfx) {
			lock (ActorVfxs) {
				if (!ActorVfxs.TryGetValue(ptr, out vfx) || vfx.Removed)
					return false;

				vfx.Removed = true;
				ActorVfxs.Remove(ptr);
				return true;
			}
		}

		public static bool TryUnregisterStatic(IntPtr ptr, out StaticVfx vfx) {
			lock (StaticVfxs) {
				if (!StaticVfxs.TryGetValue(ptr, out vfx) || vfx.Removed)
					return false;

				vfx.Removed = true;
				StaticVfxs.Remove(ptr);
				return true;
			}
		}

		#region 延迟移除

		private static readonly Lock RemoveWorkerLock = new();
		private static bool RemoveWorkerStarted;

		public static unsafe void ScheduleRemove(VfxBase? vfx, double duration) {
			if (vfx == null || vfx.Vfx == null || (IntPtr)vfx.Vfx == IntPtr.Zero || duration < 0) return;
			vfx.ExpireAtUtc = DateTime.UtcNow.AddSeconds(duration);
			EnsureRemoveWorkerStarted();
		}

		private static void EnsureRemoveWorkerStarted() {
			lock (RemoveWorkerLock) {
				if (RemoveWorkerStarted)
					return;

				RemoveWorkerStarted = true;
				ProxyPlugin.Framework.Update += RemoveWorkerLoop;
				// var thread = new Thread() {
				// 	IsBackground = true,
				// 	Name = "VFX Remove Worker"
				// };
				//
				// thread.Start();
			}
		}

		internal static void RemoveWorkerLoop(IFramework _) {
			RemoveExpiredVfxs();
		}

		private static void RemoveExpiredVfxs() {
			var now = DateTime.UtcNow;
			List<VfxBase> expired = [];
			lock (ActorVfxs)
				expired.AddRange(ActorVfxs.Values
					.Where(vfx => vfx.ExpireAtUtc.HasValue && vfx.ExpireAtUtc.Value <= now));
			lock (StaticVfxs)
				expired.AddRange(StaticVfxs.Values
					.Where(vfx => vfx.ExpireAtUtc.HasValue && vfx.ExpireAtUtc.Value <= now));
			foreach (var vfx in expired) {
				try {
					vfx.TryRemove();
				} catch (Exception ex) {
					Module.ErrorLog($"[PictoACT] 移除过期 VFX 时出错：\n{ex}");
				}
			}
		}

		#endregion 延迟移除
	}
}