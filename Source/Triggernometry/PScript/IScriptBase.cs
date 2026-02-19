using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using static Triggernometry.PScript.ScriptUtils;

namespace Triggernometry.PScript;

public abstract class IScriptBase : IActPluginV1 {
    public virtual uint? TerritoryIds() => null;
    public virtual List<TargetIcon> TargetIconList => [];
    public virtual List<StartsCasting> StartsCastingList => [];
    public virtual List<StatusAdd> StatusAddList => [];
    public virtual List<(Regex, Action<GroupCollection>)> CustomList => [];

    public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
    }

    public void DeInitPlugin() {
    }
}