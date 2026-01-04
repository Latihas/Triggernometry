using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Triggernometry.Core;
using static Triggernometry.ProxyPlugin;
using static Triggernometry.PScript.ScriptUtils.ShapeType;

// ReSharper disable ClassNeverInstantiated.Global
namespace Triggernometry.PScript;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static partial class ScriptUtils {
    public static IPlayerCharacter Me => ObjectTable.LocalPlayer;
    public static ulong Me_HexID_D() => Me.GameObjectId;
    public static ulong Me_HexID_F() => Me.GameObjectId;
    public static Vector3 Me_Position_D() => Me.Position;
    public static Vector3 Me_Position_F => Me.Position;
    public static float Me_Rotation_D() => Me.Rotation;
    public static float Me_Rotation_F => Me.Rotation;

    #region TargetIcon

    [GeneratedRegex("^.{14} TargetIcon 1B:(?<targetId>.{8}):[^:]+:.{4}:.{4}:(?<id>.{4}):")]
    private static partial Regex _LogRegexTargetIcon();

    public static readonly Regex LogRegexTargetIcon = _LogRegexTargetIcon();

    public static void MatchTargetIcon(string log, List<TargetIcon> dat) {
        var match = LogRegexTargetIcon.Match(log);
        if (match.Success)
            foreach (var d in dat) {
                var targetId = Convert.ToUInt64(match.Groups["targetId"].Value, 16);
                var id = Convert.ToInt32(match.Groups["id"].Value, 16);
                if ((d._targetId == null || d._targetId() == targetId) && (d._id == null || d._id == id))
                    d.Action(targetId, id);
            }
    }

    public record TargetIcon {
        internal readonly Func<ulong>? _targetId;
        internal int? _id;
        public readonly Action<ulong, int>? actionF;
        private readonly Action? actionN;

        public void Action(ulong targetId, int id) {
            if (actionN != null) actionN();
            else actionF!(targetId, id);
        }

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
    }

    #endregion TargetIcon

    #region StatusAdd

    [GeneratedRegex("^.{14} StatusAdd 1A:(?<effectId>[^:]+):[^:]*:[^:]*:(?<sourceId>[^:]+):[^:]*:(?<targetId>[^:]+):[^:]*:(?<count>.{2}):")]
    private static partial Regex _LogRegexStatusAdd();

    public static readonly Regex LogRegexStatusAdd = _LogRegexStatusAdd();

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
        public int? _effectId;
        public readonly Func<ulong>? _sourceId;
        public readonly Func<ulong>? _targetId;
        public int? _count;
        private readonly Action<int, ulong, ulong, int>? actionF;
        private readonly Action? actionN;

        public void Action(int effectId, ulong sourceId, ulong targetId, int count) {
            if (actionN != null) actionN();
            else actionF!(effectId, sourceId, targetId, count);
        }

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
    }

    #endregion StatusAdd

    #region StartsCasting

    [GeneratedRegex("^.{14} StartsCasting 14:.{8}:[^:]+:(?<id>[^:]+):")]
    private static partial Regex _LogRegexStartsCasting();

    public static readonly Regex LogRegexStartsCasting = _LogRegexStartsCasting();

    public static void MatchStartsCasting(string log, List<StartsCasting> dat) {
        var match = LogRegexStartsCasting.Match(log);
        if (match.Success)
            foreach (var d in dat) {
                var id = Convert.ToInt32(match.Groups["id"].Value, 16);
                if (d._id == null || d._id == id)
                    d.Action(id);
            }
    }

    public record StartsCasting {
        public int? _id;
        private readonly Action<int>? actionF;
        private readonly Action? actionN;

        public void Action(int id) {
            if (actionN != null) actionN();
            else actionF!(id);
        }

        public StartsCasting(Action<int> Action, int? Id = null) {
            _id = Id;
            actionF = Action;
        }

        public StartsCasting(Action Action, int? Id = null) {
            _id = Id;
            actionN = Action;
        }
    }

    #endregion StartsCasting

    public static void MatchAll(this IScriptBase scriptBase, string logLine) {
        MatchTargetIcon(logLine, scriptBase.TargetIconList);
        MatchStartsCasting(logLine, scriptBase.StartsCastingList);
        MatchStatusAdd(logLine, scriptBase.StatusAddList);
    }

    public static void ClearAllIGShape() {
        lock (ScriptDrawList)
            foreach (var c in ScriptDrawList)
                c.toRecycle = true;
    }

    public static Action TTS(string text, int delay = 0) => () => {
        if (delay > 0)
            Task.Run(async () => {
                await Task.Delay(delay);
                ActGlobals.oFormActMain.TTS(text);
            });
        else
            ActGlobals.oFormActMain.TTS(text);
    };

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class IGCircle : IGBase {
        internal readonly (float, float)[] _params;

        public IGCircle(Vector3 position, double r, long duration, uint? color = null)
            : this(() => position, r, duration, color) {
        }

        public IGCircle(Func<Vector3> position, double r, long duration, uint? color = null)
            : base(position, duration, Circle, color ?? 0x7FFFFF00u) {
            var r1 = (float)r;
            _params = new (float, float)[DefaultCircleSegments + 1];
            for (var i = 0; i <= DefaultCircleSegments; i++) {
                var currentRotation = i * DefaultCircleSegmentFullRotation;
                _params[i] = (r1 * MathF.Sin(currentRotation), r1 * MathF.Cos(currentRotation));
            }
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class IGCone(Func<Vector3> position, double r, Func<float> rotation, double angleRad, long duration, int? circleSegments = null, uint? color = null)
        : IGBase(position, duration, Cone, color ?? 0x7F00FFFFu) {
        public readonly float R = (float)r;
        public readonly Func<float> Rotation = rotation;
        public readonly float AngleRad = (float)angleRad;
        public readonly int CircleSegments = circleSegments ?? (int)(DefaultCircleSegments * (angleRad / (2 * MathF.PI)));

        public IGCone(Vector3 position, double r, float rotation, double angleRad, long duration, int? circleSegments = null, uint? color = null) : this(() => position, r, () => rotation, angleRad, duration, circleSegments, color) {
        }

        public IGCone(Vector3 position, double r, Func<float> rotation, double angleRad, long duration, int? circleSegments = null, uint? color = null) : this(() => position, r, rotation, angleRad, duration, circleSegments, color) {
        }

        public IGCone(Func<Vector3> position, double r, float rotation, double angleRad, long duration, int? circleSegments = null, uint? color = null) : this(position, r, () => rotation, angleRad, duration, circleSegments, color) {
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class IGLine(Func<Vector3> position, Func<Vector3> position2, long duration, int thickness = 5, uint? color = null)
        : IGBase(position, duration, Line, color ?? 0x7F0000FFu) {
        public readonly Func<Vector3> Position2 = position2;
        public readonly int Thickness = thickness;

        public IGLine(Vector3 position, Vector3 position2, long duration, int thickness = 5, uint? color = null)
            : this(() => position, () => position2, duration, thickness, color) {
        }

        public IGLine(Vector3 position, Func<Vector3> position2, long duration, int thickness = 5, uint? color = null)
            : this(() => position, position2, duration, thickness, color) {
        }

        public IGLine(Func<Vector3> position, Vector3 position2, long duration, int thickness = 5, uint? color = null)
            : this(position, () => position2, duration, thickness, color) {
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class IGRect(Func<Vector3> position, Func<Vector3> position2, long duration, int thickness = 5, uint? color = null)
        : IGBase(position, duration, Rect, color ?? 0x7F0000FFu) {
        public readonly Func<Vector3> Position2 = position2;
        public readonly int Thickness = thickness;
        public IGRect(Vector3 position, Vector3 position2, long duration, int thickness = 5, uint? color = null)
            : this(() => position, () => position2, duration, thickness, color) {
        }

        public IGRect(Vector3 position, Func<Vector3> position2, long duration, int thickness = 5, uint? color = null)
            : this(() => position, position2, duration, thickness, color) {
        }

        public IGRect(Func<Vector3> position, Vector3 position2, long duration, int thickness = 5, uint? color = null)
            : this(position, () => position2, duration, thickness, color) {
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class IGBase(Func<Vector3> position, long duration, ShapeType shapeType, uint color) {
        public readonly Func<Vector3> Position = position;
        public long EndTime = DateTime.Now.Ticks / 10000 + duration;
        public readonly ShapeType ShapeType = shapeType;
        public readonly uint Color = color;
        public bool toRecycle;
    }


    public enum ShapeType {
        Circle,
        Cone,
        Line,
        Rect
    }

    public static void DrawShape(IGBase shape) {
        lock (ScriptDrawList) ScriptDrawList.Add(shape);
    }

    public static IGameObject? GetGameObjectById(ulong id) => ObjectTable.SearchById(id);
    public static Func<Vector3> GetGameObjectById_Position(ulong id) => () => GetGameObjectById(id).Position;

    public static List<IGBase> ScriptDrawList = [];
    public static int BDLClearCount;

    public const int DefaultCircleSegments = 50;
    public const float DefaultCircleSegmentFullRotation = 2 * MathF.PI / DefaultCircleSegments;

    extension(ImDrawListPtr bdl) {
        public void DrawIGShape(IGBase shape) {
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
    }
}