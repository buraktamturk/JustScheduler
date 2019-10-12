using System.Threading;
using System.Threading.Tasks;

namespace JustScheduler {
    public interface IJob {
        Task Run(CancellationToken cancellationToken);
    }
    
    public interface IJob<in T> {
        Task Run(T data, CancellationToken cancellationToken);
    }
    
    public interface IJobDataCollector<in T> {
        Task Run(T data);
    }
}