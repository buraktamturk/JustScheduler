using JustScheduler.Implementation;
using System;

namespace JustScheduler {
    public interface IJobBaseBuilder {
        IJobBaseBuilder SetShutDownAutomatically(bool shutDownAutomatically = true);

        IJobBuilder WithRunner<T>() where T : IJob;
        IJobBuilder WithRunner<T>(Func<IServiceProvider, T> maker) where T : IJob;
        
        IJobBuilder<X> WithParameterRunner<T, X>() where T : IJob<X>;
        IJobBuilder<X> WithParameterRunner<T, X>(Func<IServiceProvider, IJob<X>> maker) where T : IJob<X>;
    }
}