using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JustScheduler.DataSources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("JustScheduler.Tests")]
namespace JustScheduler.Implementation {
    internal class JobManager : IJobManager {
        internal readonly List<Func<CancellationToken, Task>> when
            = new List<Func<CancellationToken, Task>>();

        internal Func<IServiceProvider, IJob> Activator = null;
        
        internal bool Singleton = false;
        internal bool RunOnce = false;
        
        public virtual async Task Run(IServiceScopeFactory serviceScopeFactory, CancellationToken token) {
            IJob Instance = null;
            IServiceScope serviceScope = null;
            
            Task cancelTask = Task.Delay(Timeout.Infinite, token);
            
            var Tasks = when.Select(a => a(token)).ToList();

            try {
                while (!token.IsCancellationRequested) {
                    if (RunOnce) {
                        RunOnce = false;
                    } else {
                        var When = await Task.WhenAny(new[] {cancelTask}.Union(Tasks));
                        if (When == cancelTask || token.IsCancellationRequested) {
                            return;
                        }

                        var index = Tasks.IndexOf(When);
                        Tasks[index] = when[index](token);
                    }

                    if (Singleton) {
                        if (Instance == null) {
                            serviceScope = serviceScopeFactory.CreateScope();
                            Instance = Activator(serviceScope.ServiceProvider);
                        }
                        
                        try {
                            await Instance.Run(token);
                        } catch (Exception e) {
                            serviceScope.ServiceProvider.GetRequiredService<ILogger<JobManager>>()
                                 .LogError(e, "Unhandled Exception in Job");
                        }
                    }
                    else {
                        using var scope = serviceScopeFactory.CreateScope();
                        try {
                            await Activator(scope.ServiceProvider).Run(token);
                        } catch (Exception e) {
                            scope.ServiceProvider.GetRequiredService<ILogger<JobManager>>()
                                 .LogError(e, "Unhandled Exception in Job");
                        }
                    }
                }
            }
            finally {
                serviceScope?.Dispose();
            }
        }
    }
    
    internal class JobManager<T> : IJobManager {
        internal IDataSource<T> DataSource;

        internal List<Func<IServiceProvider, IDataSource<T>>> InjectedDataSources;
        
        internal Func<IServiceProvider, IJob<T>> Activator;
        internal bool Singleton = false;

        public virtual async Task Run(IServiceScopeFactory serviceScopeFactory, CancellationToken token) {
            IJob<T> Instance = null;
            IServiceScope serviceScope = null;

            var dataSource = InjectedDataSources == null ? DataSource : new MergeDataSource<T>(
                InjectedDataSources.Select(a => a(serviceScopeFactory.CreateScope().ServiceProvider))
                                    .Union(new[] { DataSource })
                                    .ToList()
            );

            if (dataSource == null) {
                return;
            }
            
            try {
                await foreach (var data in dataSource.Run(token).WithCancellation(token)) {
                    if (Singleton) {
                        if (Instance == null) {
                            serviceScope = serviceScopeFactory.CreateScope();
                            Instance = Activator(serviceScope.ServiceProvider);
                        }

                        try {
                            await Instance.Run(data, token);
                        } catch (Exception e) {
                            serviceScope.ServiceProvider.GetRequiredService<ILogger<T>>()
                                        .LogError(e, "Unhandled Exception in " + nameof(T));
                        }
                    } else {
                        using var scope = serviceScopeFactory.CreateScope();
                        try {
                            await Activator(scope.ServiceProvider).Run(data, token);
                        } catch (Exception e) {
                            scope.ServiceProvider.GetRequiredService<ILogger<T>>()
                                        .LogError(e, "Unhandled Exception in " + nameof(T));
                        }
                    }
                }
            } finally {
                serviceScope?.Dispose();
            }
        }
    }

    internal class ConcurrentJobManager<T> : IJobManager
    {
        internal IDataSource<T> DataSource;

        internal List<Func<IServiceProvider, IDataSource<T>>> InjectedDataSources;

        internal Func<IServiceProvider, IJob<T>> Activator;
        internal ISlotProvider<T> slot;
        internal bool Singleton = false;
        internal int concurrencyLevel = 0;

        public virtual async Task Run(IServiceScopeFactory serviceScopeFactory, CancellationToken token)
        {
            IJob<T> Instance = null;
            IServiceScope serviceScope = null;

            var dataSource = InjectedDataSources == null ? DataSource : new MergeDataSource<T>(
                InjectedDataSources.Select(a => a(serviceScopeFactory.CreateScope().ServiceProvider))
                                    .Union(new[] { DataSource })
                                    .ToList()
            );

            if (dataSource == null)
            {
                return;
            }

            List<Task> runningTasks = new List<Task>();

            var dataTask = dataSource.Run(token)
                .WithCancellation(token)
                .GetAsyncEnumerator();

            try
            {
                Task slotTask = null;

                while (!token.IsCancellationRequested) {
                    while (slot.Count <= 0) { 
                        var runningsTasksTask = !runningTasks.Any() ? null : Task.WhenAny(runningTasks);

                        if(slotTask == null)
                        {
                            slotTask = slot.WatchChange(slot.Count);
                        }

                        var returnedTask = await (runningsTasksTask == null ? Task.WhenAny(slotTask) : Task.WhenAny(
                            runningsTasksTask,
                            slotTask
                        ));

                        if (returnedTask == runningsTasksTask)
                        {
                            var finishedTask = await runningsTasksTask;
                            runningTasks.Remove(finishedTask);
                        } else { // slot became available?
                            slotTask = null;
                            continue;
                        }
                    }

                    // continue to pool data and add to the list
                    var dataPool = await dataTask.MoveNextAsync();
                    if (!dataPool)
                    {
                        return;
                    }

                    if (Singleton)
                    {
                        if (Instance == null)
                        {
                            serviceScope = serviceScopeFactory.CreateScope();
                            Instance = Activator(serviceScope.ServiceProvider);
                        }

                        try
                        {
                            await this.slot.Consume(dataTask.Current);
                            runningTasks.Add(RunSingleton(dataTask.Current, Instance, serviceScope, token));
                        }
                        catch (Exception e)
                        {
                            serviceScope.ServiceProvider.GetRequiredService<ILogger<T>>()
                                        .LogError(e, "Unhandled Exception in " + nameof(T));
                        }
                    }
                    else
                    {

                        await this.slot.Consume(dataTask.Current);
                        runningTasks.Add(RunScoped(dataTask.Current, serviceScopeFactory, token));
                    }

                    runningTasks.RemoveAll(a => a.IsCompleted);
                }
            }
            finally
            {
                await dataTask.DisposeAsync();

                if (runningTasks.Any())
                {
                    await Task.WhenAll(runningTasks);
                }

                serviceScope?.Dispose();
            }
        }

        private async Task RunScoped(T data, IServiceScopeFactory serviceScopeFactory, CancellationToken token)
        {
            try { 
                using var scope = serviceScopeFactory.CreateScope();
                try
                {
                    await Activator(scope.ServiceProvider)
                        .Run(data, token);
                }
                catch (Exception e)
                {
                    scope.ServiceProvider.GetRequiredService<ILogger<T>>()
                                .LogError(e, "Unhandled Exception in " + nameof(T));
                }
            } finally
            {
                await this.slot.Release(data);
            }
        }

        private async Task RunSingleton(T data, IJob<T> instance, IServiceScope serviceScope, CancellationToken token)
        {
            try
            {
                try
                {
                    await instance
                        .Run(data, token);
                }
                catch (Exception e)
                {
                    serviceScope.ServiceProvider.GetRequiredService<ILogger<T>>()
                                .LogError(e, "Unhandled Exception in " + nameof(T));
                }
            }
            finally
            {
                await this.slot.Release(data);
            }
        }
    }
}