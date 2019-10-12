using System.Threading;
using System.Threading.Tasks;
using JustScheduler.SampleWeb.Models;
using Microsoft.Extensions.Logging;

namespace JustScheduler.SampleWeb.Services {

    public class IntegerService : IJob<int> {
        public Task Run(int data, CancellationToken cancellationToken) {
            throw new System.NotImplementedException();
        }
    }

    public class MessageService : IJob<Message> {
        private readonly ILogger<MessageService> _logger;

        public MessageService(ILogger<MessageService> logger) {
            this._logger = logger;
        }
        
        public Task Run(Message data, CancellationToken cancellationToken) {
            _logger.LogInformation("Message received: " + data.message);
            return Task.CompletedTask;
        }
    }
}