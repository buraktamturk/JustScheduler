using System.Collections.Generic;
using System.Threading;

namespace JustScheduler.DataSources {
    internal class StaticDataSource<T> : IDataSource<T> {
        private IAsyncEnumerable<T> item;

        internal StaticDataSource(IAsyncEnumerable<T> item) {
            this.item = item;
        }
        
        public IAsyncEnumerable<T> Run(CancellationToken cancellationToken) {
            return item;
        }
    }
}