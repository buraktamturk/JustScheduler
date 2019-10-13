using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustScheduler.DataSources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
}