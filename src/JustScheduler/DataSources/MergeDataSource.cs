using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JustScheduler.DataSources {
    internal class MergeDataSource<T> : IDataSource<T> {
        internal List<IDataSource<T>> sources;

        internal MergeDataSource(List<IDataSource<T>> sources) {
            this.sources = sources;
        }
        
        public async IAsyncEnumerable<T> Run(CancellationToken cancellationToken) {
            var _sources = sources.Select(a => a.Run(cancellationToken)).ToList();
            var enumerators = _sources.Select(a => a.GetAsyncEnumerator(cancellationToken)).ToList();
            var tasks = enumerators.Select(a => a.MoveNextAsync().AsTask()).ToList();
            
            try {
                while (!cancellationToken.IsCancellationRequested) {
                    var handle = await Task.WhenAny(tasks);
                    var value = await handle;
                    var index = tasks.IndexOf(handle);
                    
                    if (value) {
                        var item = enumerators[index].Current;
                        tasks[index] = enumerators[index].MoveNextAsync().AsTask();
                        yield return item;
                    } else {
                        await enumerators[index].DisposeAsync();
                        _sources.RemoveAt(index);
                        enumerators.RemoveAt(index);
                        tasks.RemoveAt(index);

                        if (tasks.Count == 0) {
                            break;
                        }
                    }
                }
            } finally {
                if(tasks.Count > 0) {
                    await Task.WhenAll(tasks);
                    await Task.WhenAll(enumerators.Select(a => a.DisposeAsync().AsTask()).ToList());
                }
            }
        }
    }
}