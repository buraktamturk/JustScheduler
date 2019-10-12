using System;
using JustScheduler.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JustScheduler {
    public static class ServiceExtensions {
        public static IServiceCollection AddJustScheduler(this IServiceCollection that, Action<IJobBaseBuilder> configure = null) {
            JobBaseBuilder builder = new JobBaseBuilder(that);

            configure?.Invoke(builder);

            return that
                .AddSingleton<IJobManager>(a => new MainJobManager(builder._jobManagers, a.GetRequiredService<ILogger<MainJobManager>>()))
                .AddHostedService<JustSchedulerBackgroundService>();
        }
    }
}