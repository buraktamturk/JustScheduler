using System.Collections.Generic;
using System.Threading;

namespace JustScheduler.DataSources {
    internal class SingleDataSource<T> : IDataSource<T> {
        private T item;

        internal SingleDataSource(T item) {
            this.item = item;
        }
        
        #pragma warning disable 1998
        public async IAsyncEnumerable<T> Run(CancellationToken cancellationToken) {
        #pragma warning restore 1998
            
            yield return item;
        }
    }
}