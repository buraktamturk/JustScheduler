using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JustScheduler.Implementation
{
    public class SlotProvider<T> : ISlotProvider<T>
    {
        private readonly Func<T, int> _func;

        private readonly List<TaskCompletionSource<int>> taskCompletionSources
            = new List<TaskCompletionSource<int>>();

        private readonly SemaphoreSlim slim = new SemaphoreSlim(1);

        public int Count { get; private set; }

        public SlotProvider(int count, Func<T, int> func = null)
        {
            this.Count = count;
            this._func = func ?? (a => 1);
        }

        public async Task Consume(T data)
        {
            int toConsume = this._func(data);

            while(Count < toConsume)
            {
                await WatchChange(Count);
            }

            await slim.WaitAsync();
            try { 
                Count -= toConsume;
                taskCompletionSources.ForEach(a => a.SetResult(Count));
                taskCompletionSources.Clear();
            } finally { 
                slim.Release();
            }
        }

        public async Task Release(T data)
        {
            await slim.WaitAsync();
            try
            {
                Count += this._func(data);
                taskCompletionSources.ForEach(a => a.SetResult(Count));
                taskCompletionSources.Clear();
            }
            finally
            {
                slim.Release();
            }
        }

        public async Task<int> WatchChange(int previousCount)
        {
            TaskCompletionSource<int> tsc;

            await slim.WaitAsync();
            try
            {
                if (previousCount != Count)
                {
                    return Count;
                }

                tsc = new TaskCompletionSource<int>();
                taskCompletionSources.Add(tsc);
            }
            finally
            {
                slim.Release();
            }

            return await tsc.Task;
        }
    }
}
