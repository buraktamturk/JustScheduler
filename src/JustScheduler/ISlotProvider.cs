using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JustScheduler
{
    public interface ISlotProvider<T>
    {
        public int Count { get; }

        Task Consume(T data);

        Task Release(T data);

        Task<int> WatchChange(int previousCount);
    }
}
