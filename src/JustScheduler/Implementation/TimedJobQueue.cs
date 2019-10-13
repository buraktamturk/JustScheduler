using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace JustScheduler.Implementation {
    internal class TimedJobQueue<T, X> {
        internal class TimedJobTrigger : ITimedJobTrigger<T, X> {
            private readonly TimedJobQueue<T, X> _queue;
            
            internal TimedJobTrigger(TimedJobQueue<T, X> queue) {
                this._queue = queue;
            }
            
            public void Trigger(X data, DateTime scheduleAt) {
                _queue._workItems.Enqueue((data, scheduleAt));
                _queue._signal.Release();
            }
        }
        
        private ConcurrentQueue<(X, DateTime)> _workItems = new ConcurrentQueue<(X, DateTime)>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);
        
        internal async Task<(X, DateTime)> DequeueAsync(CancellationToken cancellationToken) {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);
            return workItem;
        }
    }
}