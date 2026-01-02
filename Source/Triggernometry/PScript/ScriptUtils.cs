using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Colors;
using Triggernometry.Core;

namespace Triggernometry.PScript;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static partial class ScriptUtils
{
    public static IPlayerCharacter Me => ProxyPlugin.ObjectTable.LocalPlayer;
    public static int MeHexID => (int)Me.GameObjectId;

    #region TargetIcon

    [GeneratedRegex("^.{14} TargetIcon 1B:(?<targetId>.{8}):[^:]+:.{4}:.{4}:(?<id>.{4}):")]
    private static partial Regex _LogRegexTargetIcon();

    public static readonly Regex LogRegexTargetIcon = _LogRegexTargetIcon();

    public static void MatchTargetIcon(string log, List<TargetIcon> dat)
    {
        var match = LogRegexTargetIcon.Match(log);
        if (match.Success)
            foreach (var d in dat)
                if ((d.targetId == null || d.targetId == Convert.ToInt32(match.Groups["targetId"].Value, 16)) &&
                    (d.id == null || d.id == Convert.ToInt32(match.Groups["id"].Value, 16)))
                    d.Action();
    }

    public record TargetIcon
    {
        public int? targetId;
        public int? id;
        public Action<int?, int?>? actionF;
        private Action? actionN;

        public void Action()
        {
            if (actionN != null) actionN();
            else actionF!(targetId, id);
        }

        public TargetIcon(Action<int?, int?> Action, int? TargetId = null, int? Id = null)
        {
            targetId = TargetId;
            id = Id;
            actionF = Action;
        }

        public TargetIcon(Action Action, int? TargetId = null, int? Id = null)
        {
            targetId = TargetId;
            id = Id;
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
                if ((d.effectId == null || d.effectId == Convert.ToInt32(match.Groups["effectId"].Value, 16)) &&
                    (d.sourceId == null || d.sourceId == Convert.ToInt32(match.Groups["sourceId"].Value, 16)) &&
                    (d.targetId == null || d.targetId == Convert.ToInt32(match.Groups["targetId"].Value, 16)) &&
                    (d.count == null || d.count == Convert.ToInt32(match.Groups["count"].Value, 16)))
                    d.Action();
    }

    public record StatusAdd
    {
        public int? effectId;
        public int? sourceId;
        public int? targetId;
        public int? count;
        private Action<int?, int?, int?, int?>? actionF;
        private Action? actionN;

        public void Action()
        {
            if (actionN != null) actionN();
            else actionF!(effectId, sourceId, targetId, count);
        }

        public StatusAdd(Action<int?, int?, int?, int?> Action, int? EffectId = null, int? SourceId = null, int? TargetId = null, int? Count = null)
        {
            effectId = EffectId;
            sourceId = SourceId;
            targetId = TargetId;
            count = Count;
            actionF = Action;
        }

        public StatusAdd(Action Action, int? EffectId = null, int? SourceId = null, int? TargetId = null, int? Count = null)
        {
            effectId = EffectId;
            sourceId = SourceId;
            targetId = TargetId;
            count = Count;
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
                if (d.id == null || d.id == Convert.ToInt32(match.Groups["id"].Value, 16))
                    d.Action();
    }

    public record StartsCasting
    {
        public int? id;
        private Action<int?>? actionF;
        private Action? actionN;

        public void Action()
        {
            if (actionN != null) actionN();
            else actionF!(id);
        }

        public StartsCasting(Action<int?> Action, int? Id = null)
        {
            id = Id;
            actionF = Action;
        }

        public StartsCasting(Action Action, int? Id = null)
        {
            id = Id;
            actionN = Action;
        }
    }

    #endregion StartsCasting

    public static void MatchAll(this IScriptBase scriptBase, string logLine)
    {
        MatchTargetIcon(logLine, scriptBase.TargetIconList);
        MatchStartsCasting(logLine, scriptBase.StartsCastingList);
        MatchStatusAdd(logLine, scriptBase.StatusAddList);

        if (BattleFailedRegex().IsMatch(logLine)) scriptBase.DrawList.Clear();
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

    public class IGCircle(Vector3 position, float r, long duration = long.MaxValue, uint? color = null)
        : IGBase(duration, ShapeType.Circle)
    {
        public Vector3 Position = position;
        public float R = r;
        public uint Color = color ?? 0xFF0000FFu;
    }
    public class IGCone(Vector3 position, float r, float rotation,float angleRad, long duration = long.MaxValue, uint? color = null)
        : IGBase(duration, ShapeType.Circle)
    {
        public Vector3 Position = position;
        public float R = r;
        public float Rotation = rotation;
        public float AngleRad = angleRad;
        public uint Color = color ?? 0xFF0000FFu;
    }
    public class IGBase(long duration, ShapeType shapeType)
    {
        public long Duration = duration;
        public ShapeType ShapeType = shapeType;
    }

    public enum ShapeType
    {
        Circle,
        Cone,
        Line
    }

    public static long LastMs;
    public const int CircleSegments=50;
    public static ImDrawListPtr BDL => ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());

    [GeneratedRegex("^.{14} Director 21:.{8}:40000011:00:00:00:00$")]
    private static partial Regex BattleFailedRegex();
}
