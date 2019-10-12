using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace JustScheduler {
    public interface IJobManager {
        Task Run(IServiceScopeFactory scope, CancellationToken token);
    }
}