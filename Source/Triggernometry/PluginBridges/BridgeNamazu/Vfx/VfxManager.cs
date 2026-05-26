using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Triggernometry.PluginBridges.BridgeNamazu.Modules;
using TriggernometryProxy;

namespace Triggernometry.PluginBridges.BridgeNamazu.Vfx {
	internal static class VfxManager {
		private static readonly Dictionary<IntPtr, ActorVfx> _actorVfxs = new Dictionary<IntPtr, ActorVfx>();

		private static readonly Dictionary<IntPtr, StaticVfx> _staticVfxs = new Dictionary<IntPtr, StaticVfx>();

		public static IReadOnlyDictionary<IntPtr, ActorVfx> ActorVfxs {
			get {
				lock (_actorVfxs) {
					return new Dictionary<IntPtr, ActorVfx>(_actorVfxs);
				}
			}
		}

		public static IReadOnlyDictionary<IntPtr, StaticVfx> StaticVfxs {
			get {
				lock (_staticVfxs) {
					return new Dictionary<IntPtr, StaticVfx>(_staticVfxs);
				}
			}
		}

		internal static VfxModule Module => BridgeNamazu.GetModule<VfxModule>();

		public static ActorVfx CreateActor(IntPtr srcAddress, IntPtr tgtAddress, string fullPath, string tag = null) {
			return Module.ActorVfxCreate(srcAddress, tgtAddress, fullPath, tag);
		}

		public static unsafe StaticVfx InitStatic(string fullPath, string? tag = null) {
			var vfxPtr = Module.StaticVfxCreate(fullPath);

			var vfx = new StaticVfx {
				Ptr = vfxPtr,
				Path = fullPath,
				Tag = tag ?? Vfx.DefaultTag
			};

			Register(vfx);
			Module.StaticVfxRun(vfx.Ptr);

			return vfx;
		}

		public static unsafe bool Remove(Vfx? vfx) {
			if (vfx == null || vfx.Ptr == null || (IntPtr)vfx.Ptr == IntPtr.Zero)
				return false;
			if (vfx is ActorVfx actor)
				return Module.TryActorVfxRemove(actor.Ptr);
			if (vfx is StaticVfx stat)
				return Module.TryStaticVfxRemove(stat.Ptr);
			return false;
		}

		public static void Clear() {
			lock (_actorVfxs) {
				_actorVfxs.Clear();
			}

			lock (_staticVfxs) {
				_staticVfxs.Clear();
			}
		}

		public static unsafe void Register(ActorVfx? vfx) {
			if (vfx == null || vfx.Ptr == null || (IntPtr)vfx.Ptr == IntPtr.Zero)
				return;

			lock (_actorVfxs) {
				_actorVfxs[(IntPtr)vfx.Ptr] = vfx;
			}
		}

		public static unsafe void Register(StaticVfx? vfx) {
			if (vfx == null || vfx.Ptr == null || (IntPtr)vfx.Ptr == IntPtr.Zero)
				return;

			lock (_staticVfxs) {
				_staticVfxs[(IntPtr)vfx.Ptr] = vfx;
			}
		}

		public static bool TryUnregisterActor(IntPtr ptr, out ActorVfx vfx) {
			lock (_actorVfxs) {
				if (!_actorVfxs.TryGetValue(ptr, out vfx) || vfx.Removed)
					return false;

				vfx.Removed = true;
				_actorVfxs.Remove(ptr);
				return true;
			}
		}

		public static bool TryUnregisterStatic(IntPtr ptr, out StaticVfx vfx) {
			lock (_staticVfxs) {
				if (!_staticVfxs.TryGetValue(ptr, out vfx) || vfx.Removed)
					return false;

				vfx.Removed = true;
				_staticVfxs.Remove(ptr);
				return true;
			}
		}

		#region 延迟移除

		private static readonly Lock RemoveWorkerLock = new();
		private static bool RemoveWorkerStarted;

		public static unsafe void ScheduleRemove(Vfx? vfx, double duration) {
			if (vfx == null || vfx.Ptr == null || (IntPtr)vfx.Ptr == IntPtr.Zero || duration < 0) return;
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
			List<Vfx> expired = [];
			lock (_actorVfxs)
				expired.AddRange(_actorVfxs.Values
					.Where(vfx => vfx.ExpireAtUtc.HasValue && vfx.ExpireAtUtc.Value <= now));
			lock (_staticVfxs)
				expired.AddRange(_staticVfxs.Values
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