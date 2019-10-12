using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JustScheduler.SampleWeb.Services {
    public class SimpleService : IJob {
        private ILogger<SimpleService> _logger;
        
        public SimpleService(ILogger<SimpleService> logger) {
            _logger = logger;
            
            logger.LogInformation("New instance of SimpleService!");
        }
        
        public Task Run(CancellationToken cancellationToken) {
            _logger.LogInformation("Hello world!");
            return Task.CompletedTask;
        }
    }
}