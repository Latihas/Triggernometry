using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using static Triggernometry.PScript.ScriptUtils;

namespace Triggernometry.PScript;

public abstract class IScriptBase : IActPluginV1 {
	public virtual bool IsDev => false;
	public virtual string Desc => "无信息";
	public virtual List<TargetIcon> TargetIconList => [];
	public virtual List<StartsCasting> StartsCastingList => [];
	public virtual List<StatusAdd> StatusAddList => [];
	public virtual List<(Regex, Action<GroupCollection>)> CustomList => [];
	public virtual List<(Regex, Action<GroupCollection>)> IgnoreTerritory => [];
	[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
	protected readonly Dictionary<string, CancellationTokenSource?> CtsPool = [];

	protected void ResetCts() {
		foreach (var ct in CtsPool.Keys) CtsPool.DestroyCts(ct);
	}
	public virtual void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
	}

	public virtual void DeInitPlugin() {
	}

	public virtual uint[]? TerritoryIds() => null;
}