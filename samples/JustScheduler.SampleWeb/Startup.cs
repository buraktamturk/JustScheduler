using System;
using JustScheduler.SampleWeb.DataSource;
using JustScheduler.SampleWeb.Models;
using JustScheduler.SampleWeb.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JustScheduler.SampleWeb {
    public class Startup {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services) {
            services
                .AddJustScheduler(builder => {
                    builder
                        .WithRunner<SimpleService>()
                        
                        // Uncomment to see the constructor log on Console
                        .UseSingletonPattern()
                        .InjectTrigger()
                        
                        // .ScheduleOnce()
                        // .ScheduleEvery(TimeSpan.FromSeconds(10))
                        .ScheduleByCron("* * * * *")
                        .Build();

                    builder
                        .WithParameterRunner<MessageService, Message>()
                        .ScheduleOnce(new Message("Initial System Message"))
                        .ScheduleOnce(new[] {
                            new Message("Second System Message"),
                            new Message("Third System Message")
                        })
                        
                        // sends message every seconds
                        .AddDataSource<SystemMessageSource>()
                        //.AddDataSource<SystemMessageSource>()

                        // http://localhost:5000/hello?message=HelloDemo
                        .InjectTrigger()
                        .Build();
                })
                .AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello World!"); });
            });
        }
    }
}