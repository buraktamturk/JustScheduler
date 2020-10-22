using JustScheduler.Implementation;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace JustScheduler.Tests
{
    public class JobBaseBuilderTests : IDisposable
    {
        private readonly IJobBaseBuilder _jobBaseBuilder;

        public JobBaseBuilderTests()
        {
            this._jobBaseBuilder = new JobBaseBuilder(new ServiceCollection());
        }

        public void Dispose()
        {
        }

        [Fact]
        public void SetShutDownAutomatically_True()
        {
            IJobBaseBuilder jobBaseBuilder = this._jobBaseBuilder.SetShutDownAutomatically(true);

            Assert.True((jobBaseBuilder as JobBaseBuilder)?.shutDownAutomatically);
        }

        [Fact]
        public void SetShutDownAutomatically_False()
        {
            IJobBaseBuilder jobBaseBuilder = this._jobBaseBuilder.SetShutDownAutomatically(false);

            Assert.False((jobBaseBuilder as JobBaseBuilder)?.shutDownAutomatically);
        }

        [Fact]
        public void WithRunner()
        {
            IJobBuilder jobBuilder = this._jobBaseBuilder.WithRunner((IServiceCollection) => new JobTest());

            Assert.NotNull(jobBuilder);
        }

        [Fact]
        public void WithRunnerGeneric()
        {
            IJobBuilder jobBuilder = this._jobBaseBuilder.WithRunner<JobTest>();

            Assert.NotNull(jobBuilder);
        }



    }

}
