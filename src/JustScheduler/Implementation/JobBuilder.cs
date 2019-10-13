using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustScheduler.DataSources;
using Microsoft.Extensions.DependencyInjection;

namespace JustScheduler.Implementation {
    internal class JobBuilder : IJobBuilder {
        private JobBaseBuilder baseBuilder;
        private JobManager manager;
        
        internal JobBuilder(Func<IServiceProvider, IJob> maker, JobBaseBuilder baseBuilder) {
            this.baseBuilder = baseBuilder;
            
            manager = new JobManager {
                Activator = maker
            };
        }
        
        public IJobBuilder UseSingletonPattern() {
            manager.Singleton = true;
            return this;
        }

        public IJobBuilder ScheduleEvery(TimeSpan time) {
            manager.when.Add(ct => Task.Delay(time, ct));
            return this;
        }

        public IJobBuilder ScheduleEvery(Func<TimeSpan> time) {
            manager.when.Add(ct => Task.Delay(time(), ct));
            return this;
        }

        public IJobBuilder ScheduleWhen(Func<CancellationToken, Task> task) {
            manager.when.Add(task);
            return this;
        }

        public IJobBuilder ScheduleOnce() {
            manager.RunOnce = true;
            return this;
        }

        public virtual IJobBuilder InjectTrigger() {
            throw new NotImplementedException();
        }

        public IJobBaseBuilder Build() {
            baseBuilder._jobManagers.Add(manager);
            return baseBuilder;
        }
    }
    
    internal class JobBuilder<T, X> : IJobBuilder<X> where T : IJob<X> {
        private JobBaseBuilder baseBuilder;
        private JobManager<X> manager;
        
        private List<IDataSource<X>> _dataSources = new List<IDataSource<X>>();
        
        private List<Func<IServiceProvider, IDataSource<X>>> _injectedDataSources 
            = new List<Func<IServiceProvider, IDataSource<X>>>();

        internal JobBuilder(Func<IServiceProvider, IJob<X>> maker, JobBaseBuilder baseBuilder) {
            this.baseBuilder = baseBuilder;
            
            manager = new JobManager<X> {
                Activator = maker
            };
        }
        
        public IJobBuilder<X> UseSingletonPattern() {
            manager.Singleton = true;
            return this;
        }

        public IJobBuilder<X> ScheduleOnce(X parameter) {
            _dataSources.Add(new SingleDataSource<X>(parameter));
            return this;
        }

        public IJobBuilder<X> ScheduleOnce(IEnumerable<X> parameter) {
            _dataSources.Add(new EnumerableDataSource<X>(parameter));
            return this;
        }

        public IJobBuilder<X> ScheduleOnce(IAsyncEnumerable<X> parameter) {
            _dataSources.Add(new StaticDataSource<X>(parameter));
            return this;
        }

        public IJobBuilder<X> AddDataSource<Y>() where Y : IDataSource<X> {
            return AddDataSource(a => ActivatorUtilities.CreateInstance<Y>(a));
        }

        public IJobBuilder<X> AddDataSource(Func<IServiceProvider, IDataSource<X>> maker) {
            _injectedDataSources.Add(maker);
            return this;
        }

        public IJobBuilder<X> ScheduleWhen(Func<CancellationToken, Task<X>> task) {
        //    manager.when.Add(task);
            return this;
        }

        public IJobBuilder<X> InjectTrigger() {
            var queue = new JobQueue<T, X>();
            _dataSources.Add(new JobQueueDataSource<T,X>(queue));
            baseBuilder.serviceCollection.AddSingleton<IJobTrigger<T, X>>(new JobQueue<T, X>.JobTrigger(queue));
            return this;
        }

        public IJobBuilder<X> InjectTimedTrigger() {
            var queue = new TimedJobQueue<T, X>();
            _dataSources.Add(new TimedJobQueueDataSource<T,X>(queue));
            baseBuilder.serviceCollection.AddSingleton<ITimedJobTrigger<T, X>>(new TimedJobQueue<T, X>.TimedJobTrigger(queue));
            return this;
        }

        public IJobBaseBuilder Build() {
            manager.DataSource = new MergeDataSource<X>(_dataSources);
            manager.InjectedDataSources = _injectedDataSources;
            
            baseBuilder._jobManagers.Add(manager);
            return baseBuilder;
        }
    }
}