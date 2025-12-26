using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Triggernometry.Core;

namespace Triggernometry.PScript;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static partial class ScriptUtils
{
    public static IPlayerCharacter Me => ProxyPlugin.ClientState.LocalPlayer;
    public static string MeHexID => Me.GameObjectId.ToString("X");

    #region TargetIcon

    [GeneratedRegex("^.{14} TargetIcon 1B:(?<targetId>.{8}):[^:]+:.{4}:.{4}:(?<id>.{4}):")]
    private static partial Regex _LogRegexTargetIcon();

    public static readonly Regex LogRegexTargetIcon = _LogRegexTargetIcon();

    public static void MatchTargetIcon(string log, List<TargetIcon> dat)
    {
        var match = LogRegexTargetIcon.Match(log);
        if (match.Success)
        {
            foreach (var d in dat)
            {
                if ((d.targetId == null || d.targetId == match.Groups["targetId"].Value) &&
                    (d.id == null || d.id == match.Groups["id"].Value))
                    d.Action();
            }
        }
    }

    public record TargetIcon
    {
        public string? targetId;
        public string? id;
        public Action<string?, string?>? actionF;
        private Action? actionN;

        public void Action()
        {
            if (actionN != null) actionN();
            else actionF!(targetId, id);
        }

        public TargetIcon(Action<string?, string?> Action, string? TargetId = null, string? Id = null)
        {
            targetId = TargetId;
            id = Id;
            actionF = Action;
        }

        public TargetIcon(Action Action, string? TargetId = null, string? Id = null)
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
        {
            foreach (var d in dat)
            {
                if (
                    (d.effectId == null || d.effectId == match.Groups["effectId"].Value) &&
                    (d.sourceId == null || d.sourceId == match.Groups["sourceId"].Value) &&
                    (d.targetId == null || d.targetId == match.Groups["targetId"].Value) &&
                    (d.count == null || d.count == match.Groups["count"].Value))
                    d.Action();
            }
        }
    }

    public record StatusAdd
    {
        public string? effectId;
        public string? sourceId;
        public string? targetId;
        public string? count;
        private Action<string?, string?, string?, string?>? actionF;
        private Action? actionN;

        public void Action()
        {
            if (actionN != null) actionN();
            else actionF!(effectId, sourceId, targetId, count);
        }

        public StatusAdd(Action<string?, string?, string?, string?> Action, string? EffectId = null, string? SourceId = null, string? TargetId = null, string? Count = null)
        {
            effectId = EffectId;
            sourceId = SourceId;
            targetId = TargetId;
            count = Count;
            actionF = Action;
        }

        public StatusAdd(Action Action, string? EffectId = null, string? SourceId = null, string? TargetId = null, string? Count = null)
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
        {
            foreach (var d in dat)
            { 
                RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning,$"{d.id}.{match.Groups["id"].Value}");
                if (d.id == null || d.id == match.Groups["id"].Value) d.Action();
            }
        }
    }

    public record StartsCasting
    {
        public string? id;
        private Action<string?>? actionF;
        private Action? actionN;

        public void Action()
        {
            if (actionN != null) actionN();
            else actionF!(id);
        }

        public StartsCasting(Action<string?> Action, string? Id = null)
        {
            id = Id;
            actionF = Action;
        }

        public StartsCasting(Action Action, string? Id = null)
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
    }

    public static Action TTS(string text, int delay = 0) => () =>
    {
        if (delay > 0)
        {
            Task.Run(async () =>
            {
                await Task.Delay(delay);
                Advanced_Combat_Tracker.ActGlobals.oFormActMain.TTS(text);
            });
        }
        else
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.TTS(text);
    };
}
