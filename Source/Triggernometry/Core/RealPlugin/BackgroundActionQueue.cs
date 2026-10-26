using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Triggernometry.Core;

internal sealed class BackgroundActionQueue {
	private readonly BlockingCollection<WorkItem> _pending;
	private readonly Task[] _workers;
	private readonly Lock _enqueueLock = new();
	private int _completed;

	public BackgroundActionQueue() {
		var workerCount = Math.Clamp(Environment.ProcessorCount - 4, 2, 8);
		_pending = new BlockingCollection<WorkItem>(new ConcurrentQueue<WorkItem>(), Math.Max(1024, workerCount * 512));
		_workers = new Task[workerCount];
		for (var i = 0; i < workerCount; i++)
			_workers[i] = Task.Run(ProcessQueue);
	}

	public void Queue(Action action, Action onCompleted, CancellationToken cancellationToken) {
		var item = new WorkItem(action, onCompleted, cancellationToken);
		lock (_enqueueLock) {
			if (_completed != 0 || cancellationToken.IsCancellationRequested) {
				CompleteItem(onCompleted);
				return;
			}
			try {
				_pending.Add(item, cancellationToken);
			} catch (OperationCanceledException) {
				CompleteItem(onCompleted);
			} catch (InvalidOperationException) {
				CompleteItem(onCompleted);
			}
		}
	}

	private void Complete() {
		lock (_enqueueLock) {
			if (Interlocked.Exchange(ref _completed, 1) != 0) return;
			_pending.CompleteAdding();
		}
	}

	public void CompleteAndWait() {
		Complete();
		Task.WaitAll(_workers);
	}

	private void ProcessQueue() {
		foreach (var item in _pending.GetConsumingEnumerable())
			RunItem(item);
	}

	private static void RunItem(WorkItem item) {
		try {
			if (!item.CancellationToken.IsCancellationRequested) item.Action();
		} finally {
			CompleteItem(item.OnCompleted);
		}
	}

	private static void CompleteItem(Action onCompleted) {
		try {
			onCompleted();
		} catch (Exception ex) {
			RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, ex.ToString());
		}
	}

	private readonly record struct WorkItem(Action Action, Action OnCompleted, CancellationToken CancellationToken);
}