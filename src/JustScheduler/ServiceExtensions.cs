using System;
using JustScheduler.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JustScheduler {
    public static class ServiceExtensions {
        public static IServiceCollection AddJustScheduler(this IServiceCollection that, Action<IJobBaseBuilder> configure = null) {
            JobBaseBuilder builder = new JobBaseBuilder(that);

            configure?.Invoke(builder);

            return that
                .AddHostedService(a => new JustSchedulerBackgroundService(
                    a.GetRequiredService<IServiceScopeFactory>(),
                    new MainJobManager(
                        builder._jobManagers,
                        a.GetRequiredService<ILogger<MainJobManager>>(),
                        a.GetRequiredService<IHostApplicationLifetime>(),
                        builder.shutDownAutomatically
                    )
                ));
        }
    }
}