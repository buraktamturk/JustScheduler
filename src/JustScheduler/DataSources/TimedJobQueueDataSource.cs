using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JustScheduler.Implementation;

namespace JustScheduler.DataSources {
    internal class TimedJobQueueDataSource<T, X> : IDataSource<X> {
        private readonly TimedJobQueue<T,X> _queue;

        internal TimedJobQueueDataSource(TimedJobQueue<T,X> queue) {
            this._queue = queue;
        }

        internal static async Task<Z> Wait<Z>(Z param, DateTime time, CancellationToken cancellationToken = default) {
            DateTime now = DateTime.Now;
            if (now >= time) {
                return param;
            }

            await Task.Delay(time - DateTime.Now, cancellationToken);

            return param;
        }
        
        public async IAsyncEnumerable<X> Run([EnumeratorCancellation] CancellationToken cancellationToken) {
            X data;
            
            var pullTask = _queue.DequeueAsync(cancellationToken);
            List<Task<X>> datas = new List<Task<X>>();
            List<Task> onlyTask = new List<Task>();
            
            while(!cancellationToken.IsCancellationRequested) {
                try {
                    var task = await Task.WhenAny(
                        new[] {pullTask}
                            .Concat(onlyTask)
                            .ToList()
                    );

                    if (task == pullTask) {
                        (X a, DateTime b) = await pullTask;
                        pullTask = _queue.DequeueAsync(cancellationToken);
                        var _task = Wait(a, b, cancellationToken);
                        datas.Add(_task);
                        onlyTask.Add(_task);
                        continue;
                    }
                    
                    var index = onlyTask.IndexOf(task);
                    data = await datas[index];
                    
                    datas.RemoveAt(index);
                    onlyTask.RemoveAt(index);
                } catch (OperationCanceledException) {
                    break;
                }

                yield return data;
            }
        }
    }
}