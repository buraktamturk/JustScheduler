using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace JustScheduler.Implementation {
    internal class JobQueue<T, X> {
        internal class JobTrigger : IJobTrigger<T, X> {
            private readonly JobQueue<T, X> _queue;
            
            internal JobTrigger(JobQueue<T, X> queue) {
                this._queue = queue;
            }
            
            public void Trigger(X data) {
                _queue._workItems.Enqueue(data);
                _queue._signal.Release();
            }
        }
        
        private readonly ConcurrentQueue<X> _workItems = new ConcurrentQueue<X>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        
        internal async Task<X> DequeueAsync(CancellationToken cancellationToken) {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);
            return workItem;
        }
    }
}