using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Triggernometry.Core;
using static TriggernometryProxy.ProxyPlugin;
using static Triggernometry.PScript.ScriptUtils.ShapeType;

// ReSharper disable ClassNeverInstantiated.Global
namespace Triggernometry.PScript;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static partial class ScriptUtils {
	public static readonly Regex LogRegexTargetIcon = _LogRegexTargetIcon();
	public static readonly Regex LogRegexStatusAdd = _LogRegexStatusAdd();
	public static readonly Regex LogRegexStartsCasting = _LogRegexStartsCasting();
	public static Context fakectx = new(null);
	public static readonly Trigger _tri = new();
	public static IPlayerCharacter Me => ObjectTable.LocalPlayer;
	public static uint CurrentTerritory => ClientState.TerritoryType;
	public static Func<Vector3> Me_Position => () => Me.Position;
	public static Func<float> Me_Rotation => () => Me.Rotation;
	public static ulong Me_HexID() => Me.EntityId;
	public static IGameObject? GetGameObjectById(ulong id) => ObjectTable.SearchById(id);
	public static Func<Vector3> GetGameObjectById_Position(ulong id) => () => GetGameObjectById(id).Position;
	public static Func<float> GetGameObjectById_Rotation(ulong id) => () => GetGameObjectById(id).Rotation;
	public static void Log(string message) => RealPlugin.Instance.InvokeNamedCallback("command", $"/e {message}");

	public enum JobCat {
		MT,
		ST,
		H1,
		H2,
		D1,
		D2,
		D3,
		D4
	}

	public static void MatchAll(this IScriptBase scriptBase, string logLine) {
		MatchTargetIcon(logLine, scriptBase.TargetIconList);
		MatchStartsCasting(logLine, scriptBase.StartsCastingList);
		MatchStatusAdd(logLine, scriptBase.StatusAddList);
		// RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error,$"{scriptBase.GetType()}/{scriptBase.CustomList.Count}/{logLine}");
		foreach (var (regex, action) in scriptBase.CustomList) {
			var match = regex.Match(logLine);
			if (!match.Success) continue;
			action(match.Groups);
		}
	}

	public static void ClearAllIGShape() {
		lock (ScriptDrawList)
			foreach (var c in ScriptDrawList.Where(c => !c.persist))
				c.toRecycle = true;
	}

	public static float Deg2Rad(float deg) => deg * MathF.PI / 180;

	public static Action MTTS(string text, int delay = 0) => () => TTS(text, delay);

	public static void TTS(string text, int delay = 0) {
		if (delay > 0) DelayExec(() => ActGlobals.oFormActMain.TTS(text), delay);
		else ActGlobals.oFormActMain.TTS(text);
	}

	public static void Beep(float freq, int length) {
		RealPlugin.Instance.QueueAction(fakectx, _tri, null,
			new ActionOld {
				ActionType = ActionOld.ActionTypeEnum.SystemBeep,
				SystemBeepFreqExpression = freq.ToString(),
				SystemBeepLengthExpression = length.ToString()
			}, DateTime.Now, true);
	}

	public static void DelayExec(Action action, int delay = 0) {
		Task.Run(async () => {
			await Task.Delay(delay);
			action();
		});
	}

	public static Func<float> BossFacingToPlayer(ulong BossId) => () => {
		var playerPosition = Me_Position();
		var bossPosition = GetGameObjectById_Position(BossId)();
		var deltaX = playerPosition.X - bossPosition.X;
		var deltaY = playerPosition.Z - bossPosition.Z;
		if (MathF.Abs(deltaX) < 1e-6 && MathF.Abs(deltaY) < 1e-6) return 0f;
		var rad2 = MathF.Atan2(deltaX, deltaY);
		if (rad2 < 0) rad2 += 2 * MathF.PI;
		return rad2;
	};

	public static Func<float> BossFacingToTarget(ulong BossId, ulong dst) => () => {
		var bossPosition = GetGameObjectById(BossId)!.Position;
		var target = GetGameObjectById(dst)!.Position;
		var deltaX = target.X - bossPosition.X;
		var deltaY = target.Z - bossPosition.Z;
		if (MathF.Abs(deltaX) < 1e-6 && MathF.Abs(deltaY) < 1e-6) return 0f;
		var rad2 = MathF.Atan2(deltaX, deltaY);
		if (rad2 < 0) rad2 += 2 * MathF.PI;
		return rad2;
	};

	public static void ShowTexts(string[] strs) {
		Task.Run(async () => {
			foreach (var s in strs) {
				RealPlugin.Instance.InvokeNamedCallback("command", $"/e {s}");
				await Task.Delay(100);
			}
		});
	}

	#region TargetIcon

	[GeneratedRegex("^.{14} TargetIcon 1B:(?<targetId>.{8}):[^:]+:[^:]+:[^:]+:(?<id>.{4}):")]
	private static partial Regex _LogRegexTargetIcon();


	private static void MatchTargetIcon(string log, List<TargetIcon> dat) {
		var match = LogRegexTargetIcon.Match(log);
		if (!match.Success) return;
		foreach (var d in dat) {
			var targetId = Convert.ToUInt64(match.Groups["targetId"].Value, 16);
			var id = Convert.ToInt32(match.Groups["id"].Value, 16);
			if ((d._targetId == null || d._targetId() == targetId) && (d._id == null || d._id == id))
				d.Action(targetId, id);
		}
	}

	public record TargetIcon {
		internal readonly Func<ulong>? _targetId;
		public readonly Action<ulong, int>? actionF;
		private readonly Action? actionN;
		internal int? _id;

		public TargetIcon(Action<ulong, int> Action, Func<ulong>? TargetId = null, int? Id = null) {
			_targetId = TargetId;
			_id = Id;
			actionF = Action;
		}

		public TargetIcon(Action Action, Func<ulong>? TargetId = null, int? Id = null) {
			_targetId = TargetId;
			_id = Id;
			actionN = Action;
		}

		public void Action(ulong targetId, int id) {
			if (actionN != null) actionN();
			else actionF!(targetId, id);
		}
	}

	#endregion TargetIcon

	#region StatusAdd

	[GeneratedRegex("^.{14} StatusAdd 1A:(?<effectId>[^:]+):[^:]*:[^:]*:(?<sourceId>[^:]+):[^:]*:(?<targetId>[^:]+):[^:]*:(?<count>.{2}):")]
	private static partial Regex _LogRegexStatusAdd();


	public static void MatchStatusAdd(string log, List<StatusAdd> dat) {
		var match = LogRegexStatusAdd.Match(log);
		if (!match.Success) return;
		foreach (var d in dat) {
			var effectId = Convert.ToInt32(match.Groups["effectId"].Value, 16);
			var sourceId = Convert.ToUInt64(match.Groups["sourceId"].Value, 16);
			var targetId = Convert.ToUInt64(match.Groups["targetId"].Value, 16);
			var count = Convert.ToInt32(match.Groups["count"].Value, 16);
			if ((d._effectId == null || d._effectId == effectId) && (d._sourceId == null || d._sourceId() == sourceId) &&
			    (d._targetId == null || d._targetId() == targetId) && (d._count == null || d._count == count))
				d.Action(effectId, sourceId, targetId, count);
		}
	}

	public record StatusAdd {
		public readonly Func<ulong>? _sourceId;
		public readonly Func<ulong>? _targetId;
		private readonly Action<int, ulong, ulong, int>? actionF;
		private readonly Action? actionN;
		public int? _count;
		public int? _effectId;

		public StatusAdd(Action<int, ulong, ulong, int> Action, int? EffectId = null, Func<ulong>? SourceId = null, Func<ulong>? TargetId = null, int? Count = null) {
			_effectId = EffectId;
			_sourceId = SourceId;
			_targetId = TargetId;
			_count = Count;
			actionF = Action;
		}

		public StatusAdd(Action Action, int? EffectId = null, Func<ulong>? SourceId = null, Func<ulong>? TargetId = null, int? Count = null) {
			_effectId = EffectId;
			_sourceId = SourceId;
			_targetId = TargetId;
			_count = Count;
			actionN = Action;
		}

		public void Action(int effectId, ulong sourceId, ulong targetId, int count) {
			if (actionN != null) actionN();
			else actionF!(effectId, sourceId, targetId, count);
		}
	}

	#endregion StatusAdd

	#region StartsCasting

	[GeneratedRegex("^.{14} StartsCasting 14:(?<sourceId>.{8}):[^:]+:(?<id>[^:]+):[^:]+:(?<targetId>[^:]+):")]
	private static partial Regex _LogRegexStartsCasting();


	public static void MatchStartsCasting(string log, List<StartsCasting> dat) {
		var match = LogRegexStartsCasting.Match(log);
		if (!match.Success) return;
		foreach (var d in dat) {
			var sourceId = Convert.ToUInt64(match.Groups["sourceId"].Value, 16);
			var id = Convert.ToInt32(match.Groups["id"].Value, 16);
			var targetId = Convert.ToUInt64(match.Groups["targetId"].Value, 16);
			if ((d._sourceId == null || d._sourceId == sourceId) &&
			    (d._id == null || d._id == id) &&
			    (d._targetId == null || d._targetId == targetId))
				d.Action(sourceId, id, targetId);
		}
	}

	public record StartsCasting {
		private readonly Action<ulong, int, ulong>? actionF;
		private readonly Action? actionN;
		public int? _id;
		public ulong? _sourceId;
		public ulong? _targetId;

		public StartsCasting(Action<ulong, int, ulong> Action, ulong? sourceId = null, int? Id = null, ulong? targetId = null) {
			_id = Id;
			_sourceId = sourceId;
			_targetId = targetId;
			actionF = Action;
		}

		public StartsCasting(Action Action, ulong? sourceId = null, int? Id = null, ulong? targetId = null) {
			_id = Id;
			_sourceId = sourceId;
			_targetId = targetId;
			actionN = Action;
		}

		public void Action(ulong sourceId, int id, ulong targetId) {
			if (actionN != null) actionN();
			else actionF!(sourceId, id, targetId);
		}
	}

	#endregion StartsCasting

	#region Draw

	public enum ShapeType {
		Circle,
		Cone,
		Line,
		Rect,
		Ray,
		Ring,
		Dot
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public class IGBase(Func<Vector3> position, long duration, ShapeType shapeType, uint color, bool persist = false) {
		public readonly uint Color = color;
		public readonly long Duration = duration;
		public readonly long EndTime = duration == long.MaxValue ? long.MaxValue : DateTime.Now.Ticks / 10000 + duration;
		public readonly Func<Vector3> Position = position;
		public readonly ShapeType ShapeType = shapeType;
		internal bool toRecycle;
		public bool toRemove;
		public readonly bool persist = persist;

		public void Remove() => toRemove = true;
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public class IGCircle : IGBase {
		internal readonly (float, float)[] _params;

		public IGCircle(Vector3 position, double r, long duration, uint? color = null, bool persist = false)
			: this(() => position, r, duration, color, persist) {
		}

		public IGCircle(Func<Vector3> position, double r, long duration, uint? color = null, bool persist = false)
			: base(position, duration, Circle, color ?? 0x40FFFF00u, persist) {
			var r1 = (float)r;
			_params = new (float, float)[DefaultCircleSegments + 1];
			for (var i = 0; i <= DefaultCircleSegments; i++) {
				var currentRotation = i * DefaultCircleSegmentFullRotation;
				_params[i] = (r1 * MathF.Sin(currentRotation), r1 * MathF.Cos(currentRotation));
			}
		}
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public class IGCone(Func<Vector3> position, double r, Func<float> rotation, double angleRad, long duration, int? circleSegments = null, uint? color = null, bool persist = false)
		: IGBase(position, duration, Cone, color ?? 0x4000FFFFu, persist) {
		public readonly float AngleRad = (float)angleRad;
		public readonly int CircleSegments = circleSegments ?? (int)(DefaultCircleSegments * (angleRad / (2 * MathF.PI)));
		public readonly float R = (float)r;
		public readonly Func<float> Rotation = rotation;

		public IGCone(Vector3 position, double r, float rotation, double angleRad, long duration, int? circleSegments = null, uint? color = null, bool persist = false) : this(() => position, r, () => rotation, angleRad, duration, circleSegments, color,
			persist) {
		}

		public IGCone(Vector3 position, double r, Func<float> rotation, double angleRad, long duration, int? circleSegments = null, uint? color = null, bool persist = false) : this(() => position, r, rotation, angleRad, duration, circleSegments, color,
			persist) {
		}

		public IGCone(Func<Vector3> position, double r, float rotation, double angleRad, long duration, int? circleSegments = null, uint? color = null, bool persist = false) : this(position, r, () => rotation, angleRad, duration, circleSegments, color,
			persist) {
		}
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public class IGLine(Func<Vector3> position, Func<Vector3> position2, long duration, float thickness = 5, uint? color = null, bool persist = false)
		: IGBase(position, duration, Line, color ?? 0x400000FFu, persist) {
		public readonly Func<Vector3> Position2 = position2;
		public readonly float Thickness = thickness;

		public IGLine(Vector3 position, Vector3 position2, long duration, float thickness = 5, uint? color = null, bool persist = false)
			: this(() => position, () => position2, duration, thickness, color, persist) {
		}

		public IGLine(Vector3 position, Func<Vector3> position2, long duration, float thickness = 5, uint? color = null, bool persist = false)
			: this(() => position, position2, duration, thickness, color, persist) {
		}

		public IGLine(Func<Vector3> position, Vector3 position2, long duration, float thickness = 5, uint? color = null, bool persist = false)
			: this(position, () => position2, duration, thickness, color, persist) {
		}
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public class IGRect(Func<Vector3> position, Func<Vector3> position2, long duration, float thickness = 5, uint? color = null, bool persist = false)
		: IGBase(position, duration, Rect, color ?? 0x400000FFu, persist) {
		public readonly Func<Vector3> Position2 = position2;
		public readonly float Thickness = thickness;

		public IGRect(Vector3 position, Vector3 position2, long duration, float thickness = 5, uint? color = null, bool persist = false)
			: this(() => position, () => position2, duration, thickness, color, persist) {
		}

		public IGRect(Vector3 position, Func<Vector3> position2, long duration, float thickness = 5, uint? color = null, bool persist = false)
			: this(() => position, position2, duration, thickness, color, persist) {
		}

		public IGRect(Func<Vector3> position, Vector3 position2, long duration, float thickness = 5, uint? color = null, bool persist = false)
			: this(position, () => position2, duration, thickness, color, persist) {
		}
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public class IGRay(Func<Vector3> position, float length, Func<float> rotation, long duration, float thickness = 5, uint? color = null, bool persist = false)
		: IGBase(position, duration, Ray, color ?? 0x400000FFu, persist) {
		public readonly float Length = length;
		public readonly Func<float> Rotation = rotation;
		public readonly float Thickness = thickness;

		public IGRay(Vector3 position, float length, float rotation, long duration, float thickness = 5, uint? color = null, bool persist = false)
			: this(() => position, length, () => rotation, duration, thickness, color, persist) {
		}

		public IGRay(Vector3 position, float length, Func<float> rotation, long duration, float thickness = 5, uint? color = null, bool persist = false)
			: this(() => position, length, rotation, duration, thickness, color, persist) {
		}

		public IGRay(Func<Vector3> position, float length, float rotation, long duration, float thickness = 5, uint? color = null, bool persist = false)
			: this(position, length, () => rotation, duration, thickness, color, persist) {
		}
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public class IGRing(Func<Vector3> position, double R, double r, long duration, uint? color = null, bool persist = false)
		: IGBase(position, duration, Ring, color ?? 0x40FFFF00u, persist) {
		public readonly float r = (float)r;
		public readonly float R = (float)R;

		public IGRing(Vector3 position, double R, double r, long duration, uint? color = null, bool persist = false)
			: this(() => position, R, r, duration, color, persist) {
		}
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public class IGDot(Func<Vector3> position, float size, long? duration = null, uint? color = null, bool persist = false)
		: IGBase(position, duration ?? long.MaxValue, Dot, color ?? 0xFFFFFFFFu, persist) {
		public readonly float size = size;

		public IGDot(Vector3 position, float size, long? duration = null, uint? color = null, bool persist = false)
			: this(() => position, size, duration, color, persist) {
		}
	}

	public static void DrawShape(IGBase shape) {
		lock (ScriptDrawList) ScriptDrawList.Add(shape);
	}

	public static void RemoveShape(IGBase shape) {
		lock (ScriptDrawList) ScriptDrawList.Remove(shape);
	}

	public static List<IGBase> ScriptDrawList = [];
	public static int BDLClearCount;

	public const int DefaultCircleSegments = 50;
	public const float DefaultCircleSegmentFullRotation = 2 * MathF.PI / DefaultCircleSegments;

	extension(ImDrawListPtr bdl) {
		internal void DrawIGShape(IGBase shape) {
			switch (shape.ShapeType) {
				case Circle:
					bdl.DrawIGCircle((IGCircle)shape);
					break;
				case Line:
					bdl.DrawIGLine((IGLine)shape);
					break;
				case Cone:
					bdl.DrawIGCone((IGCone)shape);
					break;
				case Rect:
					bdl.DrawIGRect((IGRect)shape);
					break;
				case Ray:
					bdl.DrawIGRay((IGRay)shape);
					break;
				case Ring:
					bdl.DrawIGRing((IGRing)shape);
					break;
				case Dot:
					bdl.DrawIGDot((IGDot)shape);
					break;
			}
		}

		private void DrawIGCircle(IGCircle circle) {
			var position = circle.Position();
			for (var i = 0; i <= DefaultCircleSegments; i++) {
				var p = circle._params[i];
				GameGui.WorldToScreen(new Vector3(position.X + p.Item1, position.Y, position.Z + p.Item2), out var segment);
				bdl.PathLineTo(segment);
			}
			bdl.PathFillConvex(circle.Color);
			bdl.PathClear();
		}

		private void DrawIGCone(IGCone cone) {
			var position = cone.Position();
			var rotation = cone.Rotation() + cone.AngleRad / 2;
			var partialCircleSegmentRotation = cone.AngleRad / cone.CircleSegments;
			GameGui.WorldToScreen(position, out var originPositionOnScreen);
			bdl.PathLineTo(originPositionOnScreen);
			for (var i = 0; i <= cone.CircleSegments; i++) {
				var currentRotation = rotation - i * partialCircleSegmentRotation;
				GameGui.WorldToScreen(new Vector3(position.X + cone.R * MathF.Sin(currentRotation),
						position.Y,
						position.Z + cone.R * MathF.Cos(currentRotation)),
					out var segmentVectorOnCircle);
				bdl.PathLineTo(segmentVectorOnCircle);
			}
			bdl.PathFillConvex(cone.Color);
			bdl.PathClear();
		}

		private void DrawIGLine(IGLine line) {
			GameGui.WorldToScreen(line.Position(), out var vline);
			GameGui.WorldToScreen(line.Position2(), out var vline2);
			bdl.AddLine(vline, vline2, line.Color, line.Thickness);
		}

		private void DrawIGRect(IGRect rect) {
			var posA = rect.Position();
			var posB = rect.Position2();
			var halfThickness = rect.Thickness / 2f;
			var dirAB = posB - posA;
			if (dirAB == Vector3.Zero) return;
			dirAB = Vector3.Normalize(dirAB);
			var helperVec = new Vector3(0, 1, 0);
			if (MathF.Abs(Vector3.Dot(dirAB, helperVec)) > 0.99f)
				helperVec = new Vector3(1, 0, 0);
			var normalN = Vector3.Normalize(Vector3.Cross(dirAB, helperVec));
			var A1 = posA + normalN * halfThickness; // A点左侧顶点
			var A2 = posA - normalN * halfThickness; // A点右侧顶点
			var B1 = posB + normalN * halfThickness; // B点左侧顶点
			var B2 = posB - normalN * halfThickness; // B点右侧顶点

			GameGui.WorldToScreen(A1, out var screenA1);
			GameGui.WorldToScreen(A2, out var screenA2);
			GameGui.WorldToScreen(B1, out var screenB1);
			GameGui.WorldToScreen(B2, out var screenB2);

			bdl.PathClear();
			bdl.PathLineTo(screenA1);
			bdl.PathLineTo(screenB1);
			bdl.PathLineTo(screenB2);
			bdl.PathLineTo(screenA2);
			bdl.PathLineTo(screenA1);
			bdl.PathFillConvex(rect.Color);
			bdl.PathClear();
		}

		private void DrawIGRay(IGRay ray) {
			var startPos = ray.Position();
			var rad = ray.Rotation();
			var endPos = startPos + new Vector3(MathF.Sin(rad), 0, MathF.Cos(rad)) * ray.Length;
			bdl.DrawIGRect(new IGRect(startPos, endPos, ray.Duration, ray.Thickness, ray.Color));
		}

		private void DrawIGRing(IGRing ring) {
			var worldPosition = ring.Position();
			var outerScreenPoints = new Vector2[DefaultCircleSegments + 1];
			var innerScreenPoints = new Vector2[DefaultCircleSegments + 1];
			for (var i = 0; i <= DefaultCircleSegments; i++) {
				var currentRotation = i * DefaultCircleSegmentFullRotation;
				GameGui.WorldToScreen(new Vector3(
					worldPosition.X + ring.R * MathF.Sin(currentRotation),
					worldPosition.Y,
					worldPosition.Z + ring.R * MathF.Cos(currentRotation)
				), out outerScreenPoints[i]);
				GameGui.WorldToScreen(new Vector3(
					worldPosition.X + ring.r * MathF.Sin(currentRotation),
					worldPosition.Y,
					worldPosition.Z + ring.r * MathF.Cos(currentRotation)
				), out innerScreenPoints[i]);
			}
			bdl.PathClear();
			for (var i = 0; i < DefaultCircleSegments; i++) {
				bdl.PathLineTo(outerScreenPoints[i]);
				bdl.PathLineTo(outerScreenPoints[i + 1]);
				bdl.PathLineTo(innerScreenPoints[i + 1]);
				bdl.PathLineTo(innerScreenPoints[i]);
				bdl.PathLineTo(outerScreenPoints[i]);
				bdl.PathFillConvex(ring.Color);
				bdl.PathClear();
			}
		}

		private void DrawIGDot(IGDot dot) {
			if (GameGui.WorldToScreen(dot.Position(), out var p))
				bdl.AddCircleFilled(p, dot.size, dot.Color);
		}

		#endregion Draw
	}

	public static void Place(string expr) {
		try {
			var sb = new StringBuilder("{");
			foreach (var s in expr.Split(';')) {
				var parts = s.Split(':');
				var name = parts[0] switch {
					"1" => "One",
					"2" => "Two",
					"3" => "Three",
					"4" => "Four",
					_ => parts[0]
				};
				if (parts[1] == "clear")
					sb.Append($"\"{name}\":{{}},");
				else {
					var xy = parts[1].Split(',');
					sb.Append($"\"{name}\":{{\"X\":{xy[0]},\"Z\":{xy[1]},\"Y\":0,\"Active\":true}},");
				}
			}
			RealPlugin.Instance.InvokeNamedCallback("place", sb.Append('}').ToString());
		} catch (Exception ex) {
			Log($"Place Error({expr}):{ex.StackTrace}");
		}
	}
}