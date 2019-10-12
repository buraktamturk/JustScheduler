using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace JustScheduler.DataSources {
    internal class EnumerableDataSource<T> : IDataSource<T> {
        private readonly IEnumerable<T> items;

        internal EnumerableDataSource(IEnumerable<T> items) {
            this.items = items;
        }
        
        #pragma warning disable 1998
        public async IAsyncEnumerable<T> Run([EnumeratorCancellation] CancellationToken cancellationToken) {
        #pragma warning restore 1998
            
            foreach(var item in items) {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                yield return item;
            }
        }
    }
}