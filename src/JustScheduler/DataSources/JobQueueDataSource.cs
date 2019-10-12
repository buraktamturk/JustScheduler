using System;
using System.Collections.Generic;
using System.Threading;
using JustScheduler.Implementation;

namespace JustScheduler.DataSources {
    internal class JobQueueDataSource<T, X> : IDataSource<X> {
        private readonly JobQueue<T,X> _queue;

        internal JobQueueDataSource(JobQueue<T,X> queue) {
            this._queue = queue;
        }
        
        public async IAsyncEnumerable<X> Run(CancellationToken cancellationToken) {
            while(!cancellationToken.IsCancellationRequested) {
                X data;
                
                try {
                    data = await _queue.DequeueAsync(cancellationToken);
                } catch (OperationCanceledException) {
                    break;
                }

                yield return data;
            }
        }
    }
}