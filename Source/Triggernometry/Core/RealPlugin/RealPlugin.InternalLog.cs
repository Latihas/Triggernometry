using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Advanced_Combat_Tracker;

// ReSharper disable once CheckNamespace
namespace Triggernometry.Core;

public partial class RealPlugin {
	internal readonly bool DisableLogging = false;

	internal readonly Dictionary<DebugLevelEnum, Queue<InternalLog>> log = Enum.GetValues(typeof(DebugLevelEnum))
		.Cast<DebugLevelEnum>()
		.ToDictionary(level => level, _ => new Queue<InternalLog>());
	public readonly Queue<InternalLog> logFlattenTrn = [];
	public readonly Queue<string> logFlattenACT = [];

	public enum DebugLevelEnum {
		None,
		Error,
		Warning,
		Custom,
		Custom2,
		Info,
		Verbose,
		Inherit
	}

	public void ClearLog() {
		lock (log) {
			foreach (var pair in Instance.log) {
				pair.Value.Clear();
			}
			logFlattenTrn.Clear();
			logFlattenACT.Clear();
		}
		// ui?.ClearErrorCount();
	}

	internal void UnfilteredAddToLog(DebugLevelEnum level, string msg, Trigger trig = null)
		=> UnfilteredAddToLog(level, msg, trig, null);

	internal void UnfilteredAddToLog(DebugLevelEnum level, string msg, ActionOld action)
		=> UnfilteredAddToLog(level, msg, action?.ParentTrigger, action);

	internal void UnfilteredAddToLog(DebugLevelEnum level, string msg, Trigger trig, ActionOld action) {
		var dl = "[Triggernometry]: (U)" + msg;
		switch (level) {
			case DebugLevelEnum.Error: Log.Error(dl); break;
			case DebugLevelEnum.Warning: Log.Warning(dl); break;
			case DebugLevelEnum.None:
			case DebugLevelEnum.Custom:
			case DebugLevelEnum.Custom2:
			case DebugLevelEnum.Verbose: Log.Verbose(dl); break;
			case DebugLevelEnum.Info:
			case DebugLevelEnum.Inherit:
			default: Log.Info(dl); break;
		}
		var il = new InternalLog {
			Timestamp = DateTime.Now,
			Level = level,
			Message = msg,
			SourceTrigger = trig,
			SourceAction = action
		};
		if (!DisableLogging && Debugger.IsAttached) {
			Debug.WriteLine(il.ToString());
		}
		if (level == DebugLevelEnum.Error) {
			// ui?.IncrementErrorCount();
		}
		var queue = log[level];
		lock (queue) {
			queue.Enqueue(il);
			if (queue.Count > 30000) queue.Dequeue();
			logFlattenTrn.Enqueue(il);
			ActGlobals.oFormActMain.TrnLogQueue.Enqueue(il.ToString());
			if (cfg != null && logFlattenTrn.Count > cfg.LogFlattenMaxCount) logFlattenTrn.Dequeue();
		}
	}

	public void FilteredAddToLog(DebugLevelEnum level, string msg, Trigger trig = null)
		=> FilteredAddToLog(level, msg, trig, null);

	public void FilteredAddToLog(DebugLevelEnum level, string msg, ActionOld action)
		=> FilteredAddToLog(level, msg, action?.ParentTrigger, action);

	public void FilteredAddToLog(DebugLevelEnum level, string msg, Trigger trig, ActionOld action) {
		if (cfg != null && level > cfg.DebugLevel) {
			return;
		}
		UnfilteredAddToLog(level, msg, trig, action);
	}
}