using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JustScheduler {
    public class JustSchedulerBackgroundService : BackgroundService {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IJobManager jobManager;

        private Task Task;
        
        private CancellationTokenSource _shutdown;
        
        public JustSchedulerBackgroundService(IServiceScopeFactory serviceScopeFactory, IJobManager jobManager) {
            this.serviceScopeFactory = serviceScopeFactory;
            this.jobManager = jobManager;
        }
        
        public override Task StartAsync(CancellationToken cancellationToken) {
            _shutdown = new CancellationTokenSource();
            return base.StartAsync(cancellationToken);
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            Task = jobManager.Run(serviceScopeFactory, _shutdown.Token);
            return Task;
        }
        
        public override async Task StopAsync(CancellationToken cancellationToken) {
            _shutdown.Cancel();
            await this.Task;
        }
    }
}