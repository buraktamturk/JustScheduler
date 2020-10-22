using System;
using System.Threading;
using Xunit;

namespace JustScheduler.Tests
{
    public class JobTests
    {
        [Fact]
        public async void Run()
        {
            IJob job = new JobTest();

            await job.Run(CancellationToken.None);
        }

        [Fact]
        public async void RunGeneric()
        {
            IJob<string> job = new JobTest<string>();
            await job.Run(string.Empty, CancellationToken.None);
        }

    }
}
