using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("JustScheduler.Tests")]
namespace JustScheduler.Implementation {
    internal class JobBaseBuilder : IJobBaseBuilder {
        internal readonly List<IJobManager> _jobManagers = new List<IJobManager>();
        internal readonly IServiceCollection serviceCollection;
        internal bool shutDownAutomatically = false;

        internal JobBaseBuilder(IServiceCollection serviceCollection) {
            this.serviceCollection = serviceCollection;
        }
        
        public IJobBaseBuilder SetShutDownAutomatically(bool shutDownAutomatically = true)
        {
            this.shutDownAutomatically = shutDownAutomatically;
            return this;
        }

        public IJobBuilder WithRunner<T>() where T : IJob {
            return WithRunner(a => ActivatorUtilities.CreateInstance<T>(a));
        }

        public IJobBuilder WithRunner(Func<IServiceProvider, IJob> maker) {
            return new JobBuilder(maker, this);
        }

        public IJobBuilder<X> WithParameterRunner<T, X>() where T : IJob<X> {
            return WithParameterRunner<T, X>(a => ActivatorUtilities.CreateInstance<T>(a));
        }

        public IJobBuilder<X> WithParameterRunner<T, X>(Func<IServiceProvider, IJob<X>> maker) where T : IJob<X> {
            return new JobBuilder<T,X>(maker, this);
        }
    }
}