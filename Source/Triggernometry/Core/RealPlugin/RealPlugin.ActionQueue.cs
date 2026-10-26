using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Dalamud.Plugin.Services;
using Triggernometry.Localization;
using TriggernometryProxy;

// ReSharper disable once CheckNamespace
namespace Triggernometry.Core;

public partial class RealPlugin {
	public readonly List<QueuedAction> ActionQueue = [];
	private long curOrdinal;

	public delegate void ActionExecutionHook(Context ctx, ActionOld a);

	internal HashSet<QueuedAction> ExecutingActions = [];

	private void InitActionQueue() {
		if (cfg.StartupTriggerId != Guid.Empty) {
			if (cfg.StartupTriggerType == Configuration.StartupTriggerTypeEnum.Trigger) {
				var t = GetTriggerById(cfg.StartupTriggerId, null);
				if (t != null) {
					var le = new LogEvent {
						Text = "",
						ZoneName = "",
						Timestamp = DateTime.Now
					};
					TestTrigger(t, le, ActionOld.TriggerForceTypeEnum.SkipAll);
				}
			}
			if (cfg.StartupTriggerType == Configuration.StartupTriggerTypeEnum.Folder) {
				var f = GetFolderById(cfg.StartupTriggerId, null);
				if (f != null) {
					var le = new LogEvent {
						Text = "",
						ZoneName = "",
						Timestamp = DateTime.Now
					};
					foreach (var tx in f.Triggers) {
						TestTrigger(tx, le, ActionOld.TriggerForceTypeEnum.SkipAll);
					}
				}
			}
		}
		ProxyPlugin.Framework.Update += ActionThreadProc;
	}

	private void DeinitActionQueue() => ProxyPlugin.Framework.Update -= ActionThreadProc;

	public class QueuedAction : IComparable {
		internal DateTime when { get; set; }
		internal long ordinal { get; set; }
		internal MutexInformation? mutex { get; set; }
		internal ActionOld act { get; set; }
		internal Context ctx { get; set; }
		internal bool releaseMutex { get; set; }
		internal int _finished;

		/// <summary>
		///     The effective tag text used by the action,
		///     with expressions evaluated when the action is queued. <br />
		///     If this action’s <see cref="ActionOld.Tag" /> is not specified, or evaluates to a whitespace-string,
		///     the <see cref="Trigger.Tag" /> from the parent trigger will be used instead. <br />
		///     Empty if no tag is specified anywhere.
		/// </summary>
		internal string ParsedTag { get; set; } = "";

		public QueuedAction(DateTime when, long ordinal, MutexInformation? mtx, ActionOld act, Context ctx, bool releaseMutex) {
			this.when = when;
			this.ordinal = ordinal;
			mutex = mtx;
			this.act = act;
			this.ctx = ctx;
			this.releaseMutex = releaseMutex;
			var actionTag = ctx.EvaluateStringExpression(null, ctx, act.Tag);
			if (!string.IsNullOrWhiteSpace(actionTag)) {
				ParsedTag = actionTag;
			} else {
				var triggerTag = ctx.EvaluateStringExpression(null, ctx, ctx.Trigger?.Tag);
				if (!string.IsNullOrWhiteSpace(triggerTag))
					ParsedTag = triggerTag;
			}
		}

		public int CompareTo(object? o) {
			var b = (QueuedAction?)o;
			var ex = when.CompareTo(b?.when);
			return ex != 0 ? ex : ordinal.CompareTo(b?.ordinal);
		}

		public void ActionFinished() {
			if (Volatile.Read(ref _finished) != 0) return;
			MutexInformation? mutexToRelease = null;
			var context = ctx;
			lock (ctx.Plugin.ActionQueue) {
				if (Interlocked.CompareExchange(ref _finished, 1, 0) != 0) return;
				ctx.Plugin.ExecutingActions.Remove(this);
				var sharedMutex = mutex;
				var hasSuccessor = sharedMutex != null &&
				                   (ctx.Plugin.ActionQueue.Any(queued => queued.mutex == sharedMutex && queued.ctx.id == ctx.id) ||
				                    ctx.Plugin.ExecutingActions.Any(executing => !ReferenceEquals(executing, this) && executing.mutex == sharedMutex && executing.ctx.id == ctx.id));
				if (sharedMutex != null && releaseMutex && !hasSuccessor) {
					mutexToRelease = sharedMutex;
					mutex = null;
				} else if (sharedMutex != null && ctx.Trigger?.Sequential == true && !hasSuccessor) {
					mutexToRelease = sharedMutex;
					mutex = null;
				}
			}
			mutexToRelease?.Release(context);
		}
	}

	/// <summary>
	///     Remove queued actions from the global action queue based on the specified filter. <br />
	///     If <paramref name="filter" /> is <c>null</c>, all queued actions are cleared. <br />
	///     If any actions are removed, the action queue is re-sorted and the update event is signaled.
	/// </summary>
	/// <param name="filter">
	///     Select which queued actions to remove. <br />
	///     If <c>null</c>, all actions will be removed.
	/// </param>
	/// <returns>The number of queued actions removed from the queue.</returns>
	internal int CancelQueuedActions(Func<QueuedAction, bool>? filter = null) {
		int removedCount;
		var mutexReleases = new List<(MutexInformation Mutex, Context Context)>();
		lock (ActionQueue) {
			var toRemove = ActionQueue.Where(qa => filter == null || filter(qa)).ToList();
			removedCount = toRemove.Count;
			foreach (var qa in toRemove)
				ActionQueue.Remove(qa);
			foreach (var action in toRemove) Interlocked.CompareExchange(ref action._finished, 2, 0);

			foreach (var group in toRemove.Where(qa => qa.mutex != null)
				         .GroupBy(qa => (qa.mutex, qa.ctx.id))) {
				// Transfer the release to a surviving action, or finish the canceled chain.
				var last = ExecutingActions.Concat(ActionQueue).LastOrDefault(qa => qa.mutex == group.Key.mutex && qa.ctx.id == group.Key.id);
				if (last != null) {
					if (group.Any(qa => qa.releaseMutex)) last.releaseMutex = true;
				} else if ((group.Any(qa => qa.releaseMutex) || group.Any(qa => qa.ctx.Trigger?.Sequential == true)) && group.Key.mutex != null) {
					mutexReleases.Add((group.Key.mutex, group.First().ctx));
				}
				foreach (var action in group) action.mutex = null;
			}
		}
		foreach (var (mutex, ctx) in mutexReleases.DistinctBy(release => (release.Mutex, release.Context.id)))
			mutex.Release(ctx);
		return removedCount;
	}

	internal ActionOld? QueueActions(Context ctx, DateTime startingFrom, IEnumerable<ActionOld> actions, bool sequential, MutexInformation? mtx, Context.LoggerDelegate logger) {
		ActionOld? lastAction = null;
		var sortedActions = actions.OrderBy(a => a.OrderNumber).ToArray();
		var finalAction = sortedActions.LastOrDefault(a => a.Enabled);
		if (!sequential) {
			foreach (var action in sortedActions) {
				if (action.Enabled) {
					startingFrom = startingFrom.AddMilliseconds(ctx.EvaluateNumericExpression(logger, this, action.ExecutionDelayExpression));
					QueueAction(ctx, ctx.Trigger, mtx, action, startingFrom, finalAction == action);
					lastAction = action;
				}
			}
		} else {
			ActionOld? prev = null;
			ActionOld? first = null;
			foreach (var action in sortedActions) {
				if (!action.Enabled) {
					continue;
				}
				lastAction = action;
				if (prev != null) {
					prev.NextAction = action;
				} else {
					first = action;
					startingFrom = startingFrom.AddMilliseconds(ctx.EvaluateNumericExpression(logger, this, action.ExecutionDelayExpression));
				}
				prev = action;
			}
			if (first != null) {
				QueueAction(ctx, ctx.Trigger, mtx, first, startingFrom, false);
			}
		}
		return lastAction;
	}

	internal void ReleaseSequentialActionMutexIfUnused(Context ctx, QueuedAction action) {
		if (action.mutex == null) return;
		MutexInformation? mutexToRelease;
		lock (ActionQueue) {
			var actionMutex = action.mutex;
			if (ActionQueue.Any(queued => queued.mutex == actionMutex && queued.ctx.id == ctx.id) ||
			    ExecutingActions.Any(executing => !ReferenceEquals(executing, action) && executing.mutex == actionMutex && executing.ctx.id == ctx.id))
				return;
			if (!action.releaseMutex && ctx.Trigger?.Sequential != true) return;
			if (Interlocked.CompareExchange(ref action._finished, 1, 0) != 0) return;
			mutexToRelease = actionMutex;
			action.mutex = null;
		}
		mutexToRelease?.Release(ctx);
	}

	public void QueueAction(Context ctx, Trigger? t, MutexInformation? m, ActionOld a, DateTime when, bool releaseMutex) {
		ArgumentNullException.ThrowIfNull(ctx);
		ArgumentNullException.ThrowIfNull(a);
		ArgumentNullException.ThrowIfNull(t);
		List<(MutexInformation Mutex, Context Context)> mutexReleases = [];
		var rejected = false;
		var accepted = false;
		lock (ActionQueue) // verified
		{
			if (!a.RefireRequeue || a.RefireInterrupt) {
				var ix = ActionQueue.Where(ax => ax.act.Id == a.Id).ToArray();
				if (ix.Length != 0) {
					var rems = new List<QueuedAction>();
					if (a.RefireInterrupt) {
						rems.AddRange(ix);
						var exx = 0;
						foreach (var qa in rems) {
							ActionQueue.Remove(qa);
							Interlocked.CompareExchange(ref qa._finished, 2, 0);
							exx++;
						}
						if (exx > 0) {
							a.AddToLog(ctx, DebugLevelEnum.Verbose, I18n.Translate("internal/Plugin/actionqueuerem", "Removed {0} instance(s) of trigger '{1}' action '{2}' from queue", exx, t.LogName, a.GetDescription(ctx)));
						}
					}
					foreach (var group in rems.Where(qa => qa.mutex != null)
						         .GroupBy(qa => (qa.mutex, qa.ctx.id))) {
						var last = ExecutingActions.Concat(ActionQueue).LastOrDefault(qa => qa.mutex == group.Key.mutex && qa.ctx.id == group.Key.id);
						if (last != null) {
							if (group.Any(qa => qa.releaseMutex)) last.releaseMutex = true;
						} else if ((group.Any(qa => qa.releaseMutex) || group.Any(qa => qa.ctx.Trigger?.Sequential == true)) && group.Key.mutex != null) {
							mutexReleases.Add((group.Key.mutex, group.First().ctx));
						}
						foreach (var canceled in group) canceled.mutex = null;
					}
					if (!a.RefireRequeue) {
						a.AddToLog(ctx, DebugLevelEnum.Verbose, I18n.Translate("internal/Plugin/actionqueuefail", "Trigger '{0}' action '{1}' not queued, refire requeue disabled", t.LogName, a.GetDescription(ctx)));
						rejected = true;
					}
				}
			}
			if (!rejected) {
				long newOrdinal;
				lock (this) {
					newOrdinal = curOrdinal;
					curOrdinal++;
				}
				a.AddToLog(ctx, DebugLevelEnum.Info, I18n.Translate("internal/Plugin/actionqueued", "Queuing trigger '{0}' action '{1}' to {2} slot {3}", t.LogName, a.GetDescription(ctx), FormatDateTime(when), newOrdinal));
				ActionQueue.Add(new QueuedAction(when, newOrdinal, m, a, ctx, releaseMutex));
				ActionQueue.Sort();
				accepted = true;
			}
		}
		foreach (var (mutex, mutexContext) in mutexReleases.DistinctBy(release => (release.Mutex, release.Context.id)))
			mutex.Release(mutexContext);
		if (!accepted && releaseMutex && m != null && mutexReleases.All(release => release.Mutex != m || release.Context.id != ctx.id)) m.Release(ctx);
	}

	internal bool ReadyForOperation() => true;

	internal void ActionThreadProc(IFramework _) {
		var now = DateTime.Now;
		List<QueuedAction> ready = [];
		lock (ActionQueue) {
			while (ActionQueue.Count != 0 && ActionQueue[0].when <= now) {
				var action = ActionQueue[0];
				ActionQueue.RemoveAt(0);
				ready.Add(action);
			}
		}
		foreach (var action in ready) {
			lock (ActionQueue) {
				if (Volatile.Read(ref action._finished) == 2) continue;
				ExecutingActions.Add(action);
			}
			action.act.Execute(action, action.ctx);
		}
	}


	#region Mutex

	internal Dictionary<string, MutexInformation> mutexes = new();

	internal class MutexTicket(Context c) : IDisposable {
		internal Context ctx { get; set; } = c;
		internal ManualResetEvent ev { get; set; } = new(false);

		public void Dispose() {
			ev.Dispose();
		}
	}

	public class MutexInformation {
		private string name { get; set; }
		internal int refCount { get; set; }
		internal Context? heldBy { get; set; }
		internal List<MutexTicket> acquireQueue { get; set; }

		internal MutexInformation(string name) {
			this.name = name;
			refCount = 0;
			heldBy = null;
			acquireQueue = [];
		}

		internal MutexTicket QueueForAcquisition(Context ctx) {
			Debug.WriteLine("### {0} - Queuing acquisition for context: {1}", name, ctx);
			var m = new MutexTicket(ctx);
			lock (this) {
				acquireQueue.Add(m);
			}
			Debug.WriteLine("### {0} - Queued acquisition {1} for context: {2}", name, m.GetHashCode(), ctx);
			return m;
		}

		internal void Acquire(Context ctx) {
			Debug.WriteLine("### {0} - Acquiring for context: {1}", name, ctx);
			using var m = QueueForAcquisition(ctx);
			Acquire(ctx, m);
			Debug.WriteLine("### {0} - Acquired {1} for context: {2}", name, m.GetHashCode(), ctx);
		}

		internal void Acquire(Context ctx, MutexTicket m) {
			var start = DateTime.Now;
			var ownername = "";
			var autoget = false;
			Debug.WriteLine("### {0} - Acquisition {1} pending stage 1 for context: {2}", name, m.GetHashCode(), ctx);
			lock (this) {
				if (heldBy == null) {
					var first = acquireQueue.ElementAt(0);
					if (first == m) {
						m.ev.Set();
						refCount++;
						heldBy = ctx;
						autoget = true;
					}
				} else if (heldBy.id == ctx.id) {
					m.ev.Set();
					refCount++;
					autoget = true;
				}
				if (!autoget) {
					ownername = heldBy?.ToString();
				}
			}
			while (!m.ev.WaitOne(5000)) {
				ctx.Plugin?.FilteredAddToLog(DebugLevelEnum.Warning,
					I18n.Translate("internal/Plugin/mutexdelayed", "Context '{0}' has been waiting for mutex '{1}' on {2} for {3} ms, current owner is '{4}'", ctx.ToString(), name, m.GetHashCode(),
						(DateTime.Now - start).TotalMilliseconds, ownername));
			}
			Debug.WriteLine("### {0} - Acquisition {1} pending stage 2 for context: {2}", name, m.GetHashCode(), ctx);
			lock (this) {
				Debug.WriteLine("### {0} - Acquisition {1} pending stage 3 for context: {2}", name, m.GetHashCode(), ctx);
				acquireQueue.Remove(m);
				if (!autoget) {
					if (heldBy != null) {
						throw new InvalidOperationException(I18n.Translate("internal/Plugin/invalidacquiremutex", "Tried to acquire mutex '{0}' belonging to context '{1}' on context '{2}'", name, heldBy.ToString(), ctx.ToString()));
					}
					heldBy = ctx;
					refCount++;
					Debug.WriteLine("### {0} - New acquisition {1} for context: {2}", name, m.GetHashCode(), ctx);
				} else {
					Debug.WriteLine("### {0} - Autoget acquisition {1} for context: {2}", name, m.GetHashCode(), ctx);
				}
			}
			Debug.WriteLine("### {0} - Acquisition {1} pending stage 4 for context: {2}", name, m.GetHashCode(), ctx);
		}

		internal void Release(Context ctx) {
			Debug.WriteLine("### {0} - Releasing for context: {1}", name, ctx);
			lock (this) {
				if (heldBy == null || heldBy.id != ctx.id) {
					throw new InvalidOperationException(I18n.Translate("internal/Plugin/releaseunownedmutex", "Tried to release unowned mutex '{0}' from context '{1}'", name, ctx.ToString()));
				}
				refCount--;
				if (refCount == 0) {
					Debug.WriteLine("### {0} - Fully released by context: {1}", name, ctx);
					heldBy = null;
					WakeupNext();
				}
			}
			Debug.WriteLine("### {0} - Released for context: {1}", name, ctx);
		}

		internal void ForceRelease() {
			Debug.WriteLine("### {0} - Releasing by force", name);
			lock (this) {
				refCount = 0;
				heldBy = null;
				WakeupNext();
			}
			Debug.WriteLine("### {0} - Released by force", name);
		}

		private void WakeupNext() {
			if (acquireQueue.Count > 0) {
				var m = acquireQueue.ElementAt(0);
				Debug.WriteLine("### {0} - Waking up next context in queue : {1}", name, m.ctx);
				m.ev.Set();
			}
		}
	}

	internal MutexInformation GetMutex(string name) {
		MutexInformation mi;
		lock (mutexes) {
			if (!mutexes.TryGetValue(name, out var value)) {
				value = new MutexInformation(name);
				mutexes[name] = value;
			}
			mi = value;
		}
		return mi;
	}

	#endregion
}
