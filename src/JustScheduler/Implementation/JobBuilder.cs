using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustScheduler.DataSources;
using Microsoft.Extensions.DependencyInjection;
using NCrontab;

namespace JustScheduler.Implementation
{
    internal class JobBuilder : IJobBuilder
    {
        private readonly JobBaseBuilder baseBuilder;
        private readonly JobManager manager;

        internal JobBuilder(Func<IServiceProvider, IJob> maker, JobBaseBuilder baseBuilder)
        {
            this.baseBuilder = baseBuilder;

            manager = new JobManager
            {
                Activator = maker
            };
        }

        public IJobBuilder UseSingletonPattern()
        {
            manager.Singleton = true;
            return this;
        }

        public IJobBuilder ScheduleEvery(TimeSpan time)
        {
            manager.when.Add(ct => Task.Delay(time, ct));
            return this;
        }

        public IJobBuilder ScheduleEvery(Func<TimeSpan> time)
        {
            manager.when.Add(ct => Task.Delay(time(), ct));
            return this;
        }

        public IJobBuilder ScheduleWhen(Func<CancellationToken, Task> task)
        {
            manager.when.Add(task);
            return this;
        }

        public IJobBuilder ScheduleByCron(string cronExpression)
        {
            var cron = CrontabSchedule.Parse(cronExpression);
            manager.when.Add(ct => {
                var now = DateTime.Now;
                return Task.Delay(cron.GetNextOccurrence(now) - now, ct);
            });
            return this;
        }

        public IJobBuilder ScheduleLastDayOfMonth(int hour = 0, int minute = 0, int seconds = 0)
        {
            manager.when.Add(ct => {
                var now = DateTime.Now;
                var to = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), hour, minute, seconds);

                return Task.Delay(to - now, ct);
            });

            return this;
        }

        public IJobBuilder ScheduleLastWorkingDayOfMonth(int hour = 0, int minute = 0, int seconds = 0)
        {
            manager.when.Add(ct => {
                var now = DateTime.Now;

                var to = Enumerable.Range(1, DateTime.DaysInMonth(now.Year, now.Month))
                    .Select(day => new DateTime(now.Year, now.Month, day))
                    .Where(dt => dt.DayOfWeek != DayOfWeek.Sunday && dt.DayOfWeek != DayOfWeek.Saturday)
                    .Max(d => d.Date);

                return Task.Delay(to - now, ct);
            });

            return this;
        }

        public IJobBuilder ScheduleOnce()
        {
            manager.RunOnce = true;
            return this;
        }

        public virtual IJobBuilder InjectTrigger()
        {
            throw new NotImplementedException();
        }

        public IJobBaseBuilder Build()
        {
            baseBuilder._jobManagers.Add(manager);
            return baseBuilder;
        }
    }

    internal class JobBuilder<T, X> : IJobBuilder<X> where T : IJob<X>
    {
        private readonly JobBaseBuilder baseBuilder;

        private readonly List<IDataSource<X>> _dataSources = new List<IDataSource<X>>();
        private bool Singleton = false;
        private bool Concurrency = false;

        private readonly List<Func<IServiceProvider, IDataSource<X>>> _injectedDataSources
            = new List<Func<IServiceProvider, IDataSource<X>>>();

        private readonly Func<IServiceProvider, IJob<X>> maker;

        private ISlotProvider<X> slotProvider;

        internal JobBuilder(Func<IServiceProvider, IJob<X>> maker, JobBaseBuilder baseBuilder)
        {
            this.baseBuilder = baseBuilder;
            this.maker = maker;
        }

        public IJobBuilder<X> UseSingletonPattern()
        {
            Singleton = true;
            return this;
        }

        public IJobBuilder<X> ScheduleOnce(X parameter)
        {
            _dataSources.Add(new SingleDataSource<X>(parameter));
            return this;
        }

        public IJobBuilder<X> ScheduleOnce(IEnumerable<X> parameter)
        {
            _dataSources.Add(new EnumerableDataSource<X>(parameter));
            return this;
        }

        public IJobBuilder<X> ScheduleOnce(IAsyncEnumerable<X> parameter)
        {
            _dataSources.Add(new StaticDataSource<X>(parameter));
            return this;
        }

        public IJobBuilder<X> AddDataSource<Y>() where Y : IDataSource<X>
        {
            return AddDataSource(a => ActivatorUtilities.CreateInstance<Y>(a));
        }

        public IJobBuilder<X> AddDataSource(Func<IServiceProvider, IDataSource<X>> maker)
        {
            _injectedDataSources.Add(maker);
            return this;
        }

        public IJobBuilder<X> ScheduleWhen(Func<CancellationToken, Task<X>> task)
        {
            //    manager.when.Add(task);
            return this;
        }

        public IJobBuilder<X> InjectTrigger()
        {
            var queue = new JobQueue<T, X>();
            _dataSources.Add(new JobQueueDataSource<T, X>(queue));
            baseBuilder.serviceCollection.AddSingleton<IJobTrigger<T, X>>(new JobQueue<T, X>.JobTrigger(queue));
            return this;
        }

        public IJobBuilder<X> InjectTimedTrigger()
        {
            var queue = new TimedJobQueue<T, X>();
            _dataSources.Add(new TimedJobQueueDataSource<T, X>(queue));
            baseBuilder.serviceCollection.AddSingleton<ITimedJobTrigger<T, X>>(new TimedJobQueue<T, X>.TimedJobTrigger(queue));
            return this;
        }

        public IJobBaseBuilder Build()
        {
            if (Concurrency)
            {
                var manager = new ConcurrentJobManager<X>
                {
                    Activator = maker,
                    Singleton = this.Singleton,
                    slot = this.slotProvider
                };

                manager.DataSource = _dataSources.Count == 1 ? _dataSources.First() : new MergeDataSource<X>(_dataSources);
                manager.InjectedDataSources = _injectedDataSources;

                baseBuilder._jobManagers.Add(manager);
            }
            else
            {
                var manager = new JobManager<X>
                {
                    Activator = maker,
                    Singleton = this.Singleton
                };

                manager.DataSource = _dataSources.Count == 1 ? _dataSources.First() : new MergeDataSource<X>(_dataSources);
                manager.InjectedDataSources = _injectedDataSources;

                baseBuilder._jobManagers.Add(manager);
            }

            return baseBuilder;
        }

        public IJobBuilder<X> UseConcurrency(int level, Func<X, int> weigthPerT = null)
        {
            this.Concurrency = true;
            this.slotProvider = new SlotProvider<X>(level, weigthPerT);
            baseBuilder.serviceCollection.AddSingleton<ISlotProvider<X>>(this.slotProvider);

            return this;
        }
    }
}