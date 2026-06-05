using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Dalamud.Plugin.Services;
using Triggernometry.FFXIV;
using Triggernometry.PluginBridges.BridgeNamazu.Modules;
using Triggernometry.Utilities.Maths;
using static Triggernometry.PluginBridges.BridgeNamazu.Modules.ModuleBase;

namespace Triggernometry.PluginBridges.BridgeNamazu.Vfx;

internal static class VfxManager {
	public static readonly ConcurrentDictionary<IntPtr, ActorVfx> ActorVfxs = [];

	public static readonly ConcurrentDictionary<IntPtr, StaticVfx> StaticVfxs = [];

	internal static VfxModule Module => BridgeNamazu.GetModule<VfxModule>();

	private sealed class DelayedAction {
		public DateTime ExecuteAtUtc;
		public string Tag;
		public Action Action;
	}

	private static readonly Lock DelayedActionLock = new();
	private static readonly List<DelayedAction> DelayedActions = [];

	public static ActorVfx CreateActor(IntPtr srcAddress, IntPtr tgtAddress, string fullPath, string tag = null) => Module.ActorVfxCreate(srcAddress, tgtAddress, fullPath, tag);

	public static unsafe StaticVfx InitStatic(string fullPath, string tag = null, Action<StaticVfx> modifier = null) {
		return RunOnTick(() => {
			var vfxPtr = Module.StaticVfxCreate(fullPath);

			var vfx = new StaticVfx {
				Vfx = vfxPtr,
				Path = fullPath,
				Tag = tag ?? VfxBase.DefaultTag
			};
			StaticVfxs[(IntPtr)vfx.Vfx] = vfx;
			EnsureWorkerStarted();

			try {
				modifier?.Invoke(vfx);

				if (!vfx.Removed)
					Module.StaticVfxRun(vfx.Vfx);
				return vfx;
			} catch {
				try {
					vfx.TryRemove();
				} catch { }
				throw;
			}
		});
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
		ActorVfxs.Clear();
		StaticVfxs.Clear();
	}

	public static unsafe void Register(ActorVfx? vfx) {
		if (vfx == null || vfx.Vfx == null || (IntPtr)vfx.Vfx == IntPtr.Zero)
			return;
		ActorVfxs[(IntPtr)vfx.Vfx] = vfx;
		EnsureWorkerStarted();
	}

	public static bool TryUnregisterActor(IntPtr ptr, out ActorVfx vfx) {
		if (!ActorVfxs.TryGetValue(ptr, out vfx) || vfx.Removed)
			return false;
		vfx.Removed = true;
		ActorVfxs.Remove(ptr, out _);
		return true;
	}

	public static bool TryUnregisterStatic(IntPtr ptr, out StaticVfx vfx) {
		if (!StaticVfxs.TryGetValue(ptr, out vfx) || vfx.Removed)
			return false;
		vfx.Removed = true;
		StaticVfxs.Remove(ptr, out _);
		return true;
	}

	#region VFX 循环

	// private static Thread WorkerThread;
	// private static volatile bool WorkerStopping;
	// private static readonly object WorkerLock = new object();
	// private static bool WorkerStarted;

	public static unsafe void ScheduleRemove(VfxBase? vfx, double duration) {
		if (vfx == null || vfx.Vfx == null || (IntPtr)vfx.Vfx == IntPtr.Zero || duration < 0) return;
		vfx.ExpireAtUtc = DateTime.UtcNow.AddSeconds(duration);
		EnsureWorkerStarted();
	}

	private static void EnsureWorkerStarted() {
	}

	public static void WorkerLoop(IFramework _) {
		ProcessVfxs();
	}

	public static void Shutdown() {
		// WorkerStopping = true;
		lock (DelayedActionLock)
			DelayedActions.Clear();
		ActorVfxs.Clear();
		StaticVfxs.Clear();
	}

	/// <summary>
	///     每轮先检查并移除过期 VFX，再刷新仍然存活且依赖实体动态参数的 StaticVfx。
	/// </summary>
	private static void ProcessVfxs() {
		var now = DateTime.UtcNow;

		ExecuteDueDelayedActions(now);

		var expired = new List<VfxBase>();
		var refreshList = new List<StaticVfx>();

		expired.AddRange(ActorVfxs.Values
			.Where(vfx => vfx.ExpireAtUtc.HasValue && vfx.ExpireAtUtc.Value <= now));
		foreach (var vfx in StaticVfxs.Values) {
			if (vfx.ExpireAtUtc.HasValue && vfx.ExpireAtUtc.Value <= now) {
				expired.Add(vfx);
				continue;
			}
			if (vfx.PendingUpdate || vfx.RequiresRefresh) refreshList.Add(vfx);
		}
		RemoveExpiredVfxs(expired);
		if (refreshList.Count == 0) return;
		RefreshStaticVfxs(refreshList);
	}

	private static void ExecuteDueDelayedActions(DateTime now) {
		List<DelayedAction>? dueActions = null;

		lock (DelayedActionLock) {
			for (var i = DelayedActions.Count - 1; i >= 0; i--) {
				var item = DelayedActions[i];
				if (item.ExecuteAtUtc > now) continue;
				dueActions ??= [];
				dueActions.Add(item);
				DelayedActions.RemoveAt(i);
			}
		}

		if (dueActions == null)
			return;

		// 反转，尽量按注册顺序执行。
		foreach (var item in dueActions.Reverse<DelayedAction>()) {
			try {
				item.Action();
			} catch (Exception ex) {
				Module.ErrorLog($"[PictoACT] 执行延迟 VFX 操作时出错：\n{ex}");
			}
		}
	}

	private static void RemoveExpiredVfxs(List<VfxBase> expired) {
		foreach (var vfx in expired) {
			try {
				vfx.TryRemove();
			} catch (Exception ex) {
				Module.ErrorLog($"[PictoACT] 移除过期 VFX 时出错：\n{ex}");
			}
		}
	}

	private static unsafe void RefreshStaticVfxs(List<StaticVfx> refreshList) {
		// 有需要动态刷新的 VFX 时，缓存实体列表。
		var entities = BuildEntityMap();

		foreach (var vfx in refreshList) {
			try {
				if (vfx.Removed || vfx.Vfx == null)
					continue;

				var pendingUpdate = vfx.PendingUpdate;
				var stateApplied = ApplyResolvedStaticState(vfx, entities);

				if (pendingUpdate || stateApplied) {
					vfx.Update();
					vfx.PendingUpdate = false;
				}
			} catch (Exception ex) {
				Module.ErrorLog($"[PictoACT] 刷新动态 VFX 时出错：\n{ex}");
			}
		}
	}

	/// <summary>
	///     刷新 StaticVfx 的实际 Pos / Angles / Scales 等 VFX 属性。<br />
	///     可提供缓存的实体列表 entities，以供解析位姿和线性变换参数时查询实体坐标和朝向。<br />
	///     若未提供，则每次解析时直接查找实体。
	/// </summary>
	public static unsafe bool ApplyResolvedStaticState(StaticVfx? vfx, IReadOnlyDictionary<uint, Entity> entities) {
		if (vfx == null || vfx.Vfx == null || vfx.Removed)
			return false;

		var changed = false;
		changed |= RefreshPoseAndTransform(vfx, entities, out var distance);
		changed |= RefreshScale(vfx, distance);

		return changed;
	}

	/// <summary>
	///     自动解析位姿和线性变换参数，并写回 vfx.Pos / vfx.Angles。
	/// </summary>
	private static bool RefreshPoseAndTransform(StaticVfx vfx, IReadOnlyDictionary<uint, Entity> entities, out double? distance) {
		distance = null;

            // 防御性检验，完整创建的实体不会没有 Pos
            if (vfx.PosArg == null)
			return false;

		var pos = ResolveCoordArg(entities, vfx.PosArg);
		var angles = ResolveAngles(vfx, entities, pos, out distance);

		// 尚未指定过位姿参数，跳过后续流程（说明 Create 流程还没结束，可能出现于异步创建和修改的情况）
		if (pos == null || !angles.HasValue)
			return false;

		// 每次刷新时临时构造具体数学变换参数
		if (vfx.HasTransformArgs) {
			var transform = ResolveLinearTransformArgs(vfx, entities);
			if (transform == null)
				return false;

			vfx.Pos = (Vector3)transform.TransformCoord(pos);
			vfx.Angles = transform.TransformAngle3D(angles.Value);
		} else {
			vfx.Pos = (Vector3)pos;
			vfx.Angles = angles.Value;
		}

		return true;
	}

	/// <summary>
	///     自动解析两点位姿模式（如果指定了 Target）或单点位姿模式（如果指定了 Angle3D），并返回最终朝向。若两者都未指定，则返回 null。
	/// </summary>
	private static Vector3? ResolveAngles(StaticVfx vfx, IReadOnlyDictionary<uint, Entity> entities, XIVCoord resolvedPos, out double? distance) {
		distance = null;

		// 给定了 Target，则为两点位姿模式，以 Pos - Target 方向作为朝向，同时输出距离供后续使用。
		if (vfx.TargetArg != null) {
			if (resolvedPos == null)
				return null;

			var target = ResolveCoordArg(entities, vfx.TargetArg);
			if (target == null)
				return null;

			var theta = AngleFromTo(resolvedPos, target);
			distance = Distance(resolvedPos, target);
			return new Vector3(theta, 0, 0);
		}

		// 否则说明没有指定 Target 的单点位姿模式，如果指定了角度则返回，否则为默认值 null
		return vfx.Angle3DArg;
	}

	/// <summary>
	///     将线性变换相关参数中的实体动态参数中解析为坐标，并根据参数优先级解析为纯数学的线性变换参数。
	/// </summary>
	private static LinearTransformArgs ResolveLinearTransformArgs(StaticVfx vfx, IReadOnlyDictionary<uint, Entity> entities) {
		var transform = new LinearTransformArgs();

		// 解析坐标系中心 O
		if (vfx.TransformCenterArg != null) {
			var center = ResolveCoordArg(entities, vfx.TransformCenterArg);
			if (center == null)
				return null;

			transform.Center = center;
		}

		// 如果坐标系正北以坐标形式指定，以 O - North 解析正北角度
		if (vfx.TransformNorthCoordArg != null) {
			var center = transform.Center;
			var target = ResolveCoordArg(entities, vfx.TransformNorthCoordArg);
			if (target == null)
				return null;

			transform.Rotation = AngleFromTo(center, target);
		}
		// 如果坐标系正北以角度形式指定，直接使用
		else if (vfx.TransformNorthAngle.HasValue) {
			transform.Rotation = vfx.TransformNorthAngle.Value;
		}
		// 如果没指定坐标系正北，但中心是动态实体，则以该实体当前朝向作为坐标系正北
		else if (vfx.TransformCenterArg?.IsDynamic == true) {
			var heading = ResolveEntityHeading(entities, vfx.TransformCenterArg.EntityId);
			if (!heading.HasValue)
				return null;

			transform.Rotation = heading.Value;
		}

		// 写入 KeepX / KeepY
		if (vfx.TransformKeepX.HasValue)
			transform.KeepX = vfx.TransformKeepX.Value;

		if (vfx.TransformKeepY.HasValue)
			transform.KeepY = vfx.TransformKeepY.Value;

		return transform;
	}

	private static bool RefreshScale(StaticVfx vfx, double? distance) {
		if (vfx.ScaleArg == null)
			return false;

            if (vfx.ScaleArg.HasDistanceToken)
            {
                if (vfx.TargetArg == null)
				return false;

			if (!distance.HasValue)
				return false;
		}

		vfx.Scales = vfx.ScaleArg.Resolve(distance ?? 0.0);
		return true;
	}

	private static XIVCoord ResolveCoordArg(IReadOnlyDictionary<uint, Entity> entities, DynamicCoordArg arg) {
		if (arg == null)
			return null;

		if (arg.IsFixed)
			return arg.FixedCoord.Duplicate();

		if (entities?.TryGetValue(arg.EntityId, out var entity) != true) {
			entity = Entity.GetEntityByID(arg.EntityId);
		}

		if (entity == null)
			return null;

		return new CartesianCoord(entity.PosX, entity.PosY, entity.PosZ);
	}

	private static float? ResolveEntityHeading(IReadOnlyDictionary<uint, Entity> entities, uint entityId) {
		if (entities?.TryGetValue(entityId, out var entity) != true) {
			entity = Entity.GetEntityByID(entityId);
		}

		if (entity == null)
			return null;

		return entity.Heading;
	}

	/// <summary>
	///     扫描当前实体列表，并建立 EntityId 到完整 Entity 对象的映射。
	///     后续每个 VFX 刷新时直接从这个字典取实体，再按需读取 Pos / Heading / TargetID 等字段。
	/// </summary>
	private static Dictionary<uint, Entity> BuildEntityMap() {
		var result = new Dictionary<uint, Entity>();

		foreach (var entity in Entity.GetEntities()) {
			if (entity == null)
				continue;

			var id = entity.ID;
			if (id == 0)
				continue;

			result[id] = entity;
		}

		return result;
	}

	private static double Distance(XIVCoord? a, XIVCoord? b) {
		if (a == null || b == null)
			return 0;

		var va = (Vector3)a;
		var vb = (Vector3)b;
		return Vector3.Distance(va, vb);
	}

	private static float AngleFromTo(XIVCoord? from, XIVCoord? to) {
		if (from == null || to == null)
			return (float)Math.PI;

		var a = (Vector3)from;
		var b = (Vector3)to;

		return (float)Math.Atan2(b.X - a.X, b.Y - a.Y);
	}

	public static void ScheduleDelayedAction(string tag, double delaySeconds, Action action) {
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		if (delaySeconds < 0)
			delaySeconds = 0;

		var item = new DelayedAction {
			Tag = tag ?? VfxBase.DefaultTag,
			ExecuteAtUtc = DateTime.UtcNow.AddSeconds(delaySeconds),
			Action = action
		};

		lock (DelayedActionLock) DelayedActions.Add(item);
		

		EnsureWorkerStarted();
	}

	public static void CancelDelayedActions(Func<string, bool> tagFilter) {
		if (tagFilter == null)
			return;

		lock (DelayedActionLock) {
			for (var i = DelayedActions.Count - 1; i >= 0; i--) {
				var item = DelayedActions[i];
				if (tagFilter(item.Tag)) {
					DelayedActions.RemoveAt(i);
				}
			}
		}
	}

	#endregion VFX 循环
}