using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JustScheduler {
    public interface IJobBuilder {
        IJobBuilder UseSingletonPattern();

        IJobBuilder ScheduleEvery(TimeSpan time);
        IJobBuilder ScheduleEvery(Func<TimeSpan> time);
        IJobBuilder ScheduleWhen(Func<CancellationToken, Task> task);
        IJobBuilder ScheduleOnce();
        IJobBuilder InjectTrigger();
        
        IJobBaseBuilder Build();
    }
    
    public interface IJobBuilder<T> {
        IJobBuilder<T> UseSingletonPattern();

        IJobBuilder<T> ScheduleOnce(T parameter);
        IJobBuilder<T> ScheduleOnce(IEnumerable<T> parameter);
        IJobBuilder<T> ScheduleOnce(IAsyncEnumerable<T> parameter);

        IJobBuilder<T> AddDataSource<Y>() where Y : IDataSource<T>;
        IJobBuilder<T> AddDataSource(Func<IServiceProvider, IDataSource<T>> maker);

        IJobBuilder<T> ScheduleWhen(Func<CancellationToken, Task<T>> task);

        IJobBuilder<T> InjectTrigger();
        
        IJobBaseBuilder Build();
    }
}