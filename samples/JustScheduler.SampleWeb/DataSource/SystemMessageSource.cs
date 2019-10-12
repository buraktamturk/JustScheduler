using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JustScheduler.SampleWeb.Models;
using Microsoft.Extensions.Logging;

namespace JustScheduler.SampleWeb.DataSource {
    public class SystemMessageSource : IDataSource<Message> {
        private ILogger<SystemMessageSource> _logger;

        public SystemMessageSource(ILogger<SystemMessageSource> logger) {
            _logger = logger;
        }
        
        public async IAsyncEnumerable<Message> Run([EnumeratorCancellation] CancellationToken cancellationToken) {
            int sequence = 0;
            
            while (!cancellationToken.IsCancellationRequested) {
                ++sequence;
                _logger.LogInformation($"Sending Message Number: {sequence}");
                yield return new Message($"System Message Number: {sequence}");

                try {
                    await Task.Delay(1000, cancellationToken);
                } catch (TaskCanceledException) {
                    break;
                }
            }
        }
    }
}