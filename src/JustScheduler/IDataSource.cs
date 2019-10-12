using System.Collections.Generic;
using System.Threading;

namespace JustScheduler {
    public interface IDataSource<T> {
        IAsyncEnumerable<T> Run(CancellationToken cancellationToken);
    }
}