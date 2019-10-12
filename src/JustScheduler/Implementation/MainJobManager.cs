using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JustScheduler.Implementation {
    internal class MainJobManager : IJobManager {
        private readonly List<IJobManager> _jobManagers;
        private readonly ILogger<MainJobManager> _logger;

        internal MainJobManager(List<IJobManager> jobManagers, ILogger<MainJobManager> logger) {
            _jobManagers = jobManagers;
            _logger = logger;
        }
        
        public async Task Run(IServiceScopeFactory scope, CancellationToken token) {
            var Tasks = _jobManagers.Select(a => a.Run(scope, token)).ToList();
            
            try {
                while (Tasks.Count > 0 && !token.IsCancellationRequested) {
                    var ReturnedTask = await Task.WhenAny(Tasks);
                    if (token.IsCancellationRequested) continue;

                    try {
                        await ReturnedTask;
                    } catch (Exception e) {
                        _logger.LogError(e, "Exception in JobManager");
                    } finally {
                        int index = Tasks.IndexOf(ReturnedTask);
                        _logger.LogInformation($"JobManager at index #{index} has shut down!");

                        _jobManagers.RemoveAt(index);
                        Tasks.RemoveAt(index);
                    }
                }
            } finally {
                if(Tasks.Count > 0) {
                    await Task.WhenAll(Tasks);
                }
                
                _logger.LogInformation($"MainJobManager has shut down!");
            }
        }
    }
}