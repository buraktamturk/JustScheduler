using System;
using System.Threading;
using System.Threading.Tasks;

namespace JustScheduler.Tests
{
    internal class JobTest : IJob
    {
        public Task Run(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    internal class JobTest<T> : IJob<T>
    {
        public Task Run(T data, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

}
