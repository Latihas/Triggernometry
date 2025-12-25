using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.SubKinds;

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

    public static void MatchTargetIcon(string log, IEnumerable<TargetIcon> dat)
    {
        var match = LogRegexTargetIcon.Match(log);
        if (match.Success)
        {
            foreach (var d in dat)
            {
                if ((d.targetId == null || d.targetId == match.Groups["targetId"].Value) &&
                    (d.id == null || d.id == match.Groups["id"].Value))
                    d.action(d.targetId, d.id);
            }
        }
    }

    public record TargetIcon(Action<string?, string?> action, string? targetId = null, string? id = null)
    {
        public string? targetId = targetId;
        public string? id = id;
        public Action<string?, string?> action = action;
    }

    #endregion TargetIcon

    // #region StatusAdd

    [GeneratedRegex("^.{14} StatusAdd 1A:(?<effectId>[^:]+):[^:]*:[^:]*:(?<sourceId>[^:]+):[^:]*:(?<targetId>[^:]+):[^:]*:(?<count>.{2}):")]
    private static partial Regex _LogRegexStatusAdd();

    public static readonly Regex LogRegexStatusAdd = _LogRegexStatusAdd();

    public static void MatchStatusAdd(string log, IEnumerable<StatusAdd> dat)
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
                    d.action(d.effectId, d.sourceId, d.targetId, d.count);
            }
        }
    }

    public record StatusAdd(Action<string?, string?, string?, string?> action, string? effectId = null, string? sourceId = null, string? targetId = null, string? count = null)
    {
        public string? effectId = effectId;
        public string? sourceId = sourceId;
        public string? targetId = targetId;
        public string? count = count;
        public Action<string?, string?, string?, string?> action = action;
    }


    public static Action<string?, string?> TTS(string text, int delay = 0) => (_, _) =>
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
