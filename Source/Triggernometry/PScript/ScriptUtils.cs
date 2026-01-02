using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using static Triggernometry.PScript.ScriptUtils.ShapeType;

// ReSharper disable ClassNeverInstantiated.Global
namespace Triggernometry.PScript;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static partial class ScriptUtils
{
    public static IPlayerCharacter Me => ProxyPlugin.ObjectTable.LocalPlayer;
    public static ulong Me_HexID() => Me.GameObjectId;
    public static Vector3 Me_Position() => Me.Position;
    public static float Me_Rotation() => Me.Rotation;

    #region TargetIcon

    [GeneratedRegex("^.{14} TargetIcon 1B:(?<targetId>.{8}):[^:]+:.{4}:.{4}:(?<id>.{4}):")]
    private static partial Regex _LogRegexTargetIcon();

    public static readonly Regex LogRegexTargetIcon = _LogRegexTargetIcon();

    public static void MatchTargetIcon(string log, List<TargetIcon> dat)
    {
        var match = LogRegexTargetIcon.Match(log);
        if (match.Success)
            foreach (var d in dat)
            {
                var targetId = Convert.ToUInt64(match.Groups["targetId"].Value, 16);
                var id = Convert.ToInt32(match.Groups["id"].Value, 16);
                if ((d._targetId == null || d._targetId() == targetId) && (d._id == null || d._id == id))
                    d.Action(targetId, id);
            }
    }

    public record TargetIcon
    {
        internal readonly Func<ulong>? _targetId;
        internal int? _id;
        public readonly Action<ulong, int>? actionF;
        private readonly Action? actionN;

        public void Action(ulong targetId, int id)
        {
            if (actionN != null) actionN();
            else actionF!(targetId, id);
        }

        public TargetIcon(Action<ulong, int> Action, Func<ulong>? TargetId = null, int? Id = null)
        {
            _targetId = TargetId;
            _id = Id;
            actionF = Action;
        }

        public TargetIcon(Action Action, Func<ulong>? TargetId = null, int? Id = null)
        {
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

    public static void MatchStatusAdd(string log, List<StatusAdd> dat)
    {
        var match = LogRegexStatusAdd.Match(log);
        if (match.Success)
            foreach (var d in dat)
            {
                var effectId = Convert.ToInt32(match.Groups["effectId"].Value, 16);
                var sourceId = Convert.ToUInt64(match.Groups["sourceId"].Value, 16);
                var targetId = Convert.ToUInt64(match.Groups["targetId"].Value, 16);
                var count = Convert.ToInt32(match.Groups["count"].Value, 16);
                if ((d._effectId == null || d._effectId == effectId) && (d._sourceId == null || d._sourceId() == sourceId) &&
                    (d._targetId == null || d._targetId() == targetId) && (d._count == null || d._count == count))
                    d.Action(effectId, sourceId, targetId, count);
            }
    }

    public record StatusAdd
    {
        public int? _effectId;
        public readonly Func<ulong>? _sourceId;
        public readonly Func<ulong>? _targetId;
        public int? _count;
        private readonly Action<int, ulong, ulong, int>? actionF;
        private readonly Action? actionN;

        public void Action(int effectId, ulong sourceId, ulong targetId, int count)
        {
            if (actionN != null) actionN();
            else actionF!(effectId, sourceId, targetId, count);
        }

        public StatusAdd(Action<int, ulong, ulong, int> Action, int? EffectId = null, Func<ulong>? SourceId = null, Func<ulong>? TargetId = null, int? Count = null)
        {
            _effectId = EffectId;
            _sourceId = SourceId;
            _targetId = TargetId;
            _count = Count;
            actionF = Action;
        }

        public StatusAdd(Action Action, int? EffectId = null, Func<ulong>? SourceId = null, Func<ulong>? TargetId = null, int? Count = null)
        {
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

    public static void MatchStartsCasting(string log, List<StartsCasting> dat)
    {
        var match = LogRegexStartsCasting.Match(log);
        if (match.Success)
            foreach (var d in dat)
            {
                var id = Convert.ToInt32(match.Groups["id"].Value, 16);
                if (d._id == null || d._id == id)
                    d.Action(id);
            }
    }

    public record StartsCasting
    {
        public int? _id;
        private readonly Action<int>? actionF;
        private readonly Action? actionN;

        public void Action(int id)
        {
            if (actionN != null) actionN();
            else actionF!(id);
        }

        public StartsCasting(Action<int> Action, int? Id = null)
        {
            _id = Id;
            actionF = Action;
        }

        public StartsCasting(Action Action, int? Id = null)
        {
            _id = Id;
            actionN = Action;
        }
    }

    #endregion StartsCasting

    public static void MatchAll(this IScriptBase scriptBase, string logLine)
    {
        MatchTargetIcon(logLine, scriptBase.TargetIconList);
        MatchStartsCasting(logLine, scriptBase.StartsCastingList);
        MatchStatusAdd(logLine, scriptBase.StatusAddList);
    }

    public static void ClearAllIGShape()
    {
        lock (ScriptDrawList)
            foreach (var c in ScriptDrawList)
                c.toRecycle = true;
    }

    public static Action TTS(string text, int delay = 0) => () =>
    {
        if (delay > 0)
            Task.Run(async () =>
            {
                await Task.Delay(delay);
                Advanced_Combat_Tracker.ActGlobals.oFormActMain.TTS(text);
            });
        else
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.TTS(text);
    };

    public class IGCircle(Func<Vector3> position, float r, long duration, uint? color = null)
        : IGBase(position, duration, Circle, color)
    {
        public readonly float R = r;
    }

    public class IGCone(Func<Vector3> position, float r, float rotation, float angleRad, long duration, int circleSegments = 50, uint? color = null)
        : IGBase(position, duration, Cone, color)
    {
        public readonly float R = r;
        public readonly float Rotation = rotation;
        public readonly float AngleRad = angleRad;
        public readonly int CircleSegments = circleSegments;
    }

    public class IGLine(Func<Vector3> position, Func<Vector3> position2, long duration, int thickness = 2, uint? color = null)
        : IGBase(position, duration, Line, color)
    {
        public Vector3 Position2 => position2();
        public int Thickness = thickness;
    }

    public class IGBase(Func<Vector3> position, long duration, ShapeType shapeType, uint? color)
    {
        public Vector3 Position => position();
        public readonly long EndTime = DateTime.Now.Ticks / 10000 + duration;
        public readonly ShapeType ShapeType = shapeType;
        public readonly uint Color = color ?? 0xFF0000FFu;
        public bool toRecycle;
    }

    public enum ShapeType
    {
        Circle,
        Cone,
        Line,
    }

    public static IGameObject? GetGameObjectById(ulong id) => ProxyPlugin.ObjectTable.SearchById(id);
    public static Func<Vector3> GetGameObjectById_Position(ulong id) => () => GetGameObjectById(id).Position;
    public static List<IGBase> ScriptDrawList = [];
    public static int BDLClearCount;
}
