using System;
using Triggernometry.Localization;

namespace Triggernometry.Core;

public sealed class InternalLog {
    public DateTime Timestamp { get; set; }
    public RealPlugin.DebugLevelEnum Level { get; set; }
    public string Message { get; set; }
    public Trigger SourceTrigger { get; set; }
    public ActionOld SourceAction { get; set; }
    internal static ActionOld RecordedAction { get; set; }

    public override string ToString() => RealPlugin.FormatDateTime(Timestamp) + " - " + I18n.Translate($"LogForm/chk{Level}", $"{Level}") + " - " + Message;
}