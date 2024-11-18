using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace JustScheduler.Tests
{
    public class JobTests
    {
        [Fact]
        public async Task Run()
        {
            IJob job = new JobTest();

            await job.Run(CancellationToken.None);
        }

        [Fact]
        public async Task RunGeneric()
        {
            IJob<string> job = new JobTest<string>();
            await job.Run(string.Empty, CancellationToken.None);
        }
    }
}
