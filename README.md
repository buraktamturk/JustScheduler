## ⚠️ Migration Notice

This repository has been moved to https://codeberg.org/robinsoft/JustScheduler

All further developments of this library will continue there.

# JustScheduler

.NET Core Background Scheduler that supports multiple backends for 21st century! Built on HostedServices and supports dependency injection that is already present on your application!

Sample project can be found at samples\JustScheduler.SampleWeb directory.

## Simple Job

```csharp
public class HelloWorldBackgroundService : IJob {
  private readonly DbContext db;

  public HelloWorldBackgroundService(DbContext db) {
		this.db = db;
  }

  public async Task Run() {
		Console.WriteLine("Hello world!");
  }
}
```

## Generic Event Job

```csharp
public class SomeEventService : IJob<SomeEvent> {
  private readonly DbContext db;

  public SomeEventService(DbContext db) {
		this.db = db;
  }

  public async Task Run(SomeEvent e) {
		Console.WriteLine(e.message);
  }
}
```

## Generic Data Source

```csharp
public class RabbitMqPooler : IDataSource<SomeEvent> {
  private readonly RabbitMqService rabbitMqService;

  public SomeEventService(RabbitMqService rabbitMqService) {
		this.rabbitMqService = rabbitMqService;
  }

  public async IAsyncEnumarable<SomeEvent> Run(CancellationToken ct) {
		while(!ct.IsCancellationRequested) {
			var data = await es.PoolData(ct);
			yield return data;
		}
  }
}
```

## Startup configuration

```csharp
public void ConfigureServices(IServiceCollection services) {
  services.AddJustScheduler(builder => {
		builder
			.WithRunner<HelloWorldBackgroundService>()

			// skip this if you want HelloWorldBackgroundService to be constructed
			// every time it is scheduled to run
			// .UseSingletonPattern()

			.ScheduleEvery(TimeSpan.FromSeconds(10))
			.ScheduleOnce()

			// Schedule the tasks by crontab expression.
			.ScheduleByCron("* * * * *")

			// Schedule the task on last day of the month
			.ScheduleLastDayOfMonth(hour: 0, minute: 0, seconds: 0)

			// Schedule the task on last working day of the month
			.ScheduleLastWorkingDayOfMonth(hour: 0, minute: 0, seconds: 0)

			.InjectTrigger()
			.Build();

		builder
			.WithParameterRunner<SomeEventService, SomeEvent>()

			// skip this if you want SomeEventService to be constructed
			// every time it is scheduled to run
			// .UseSingletonPattern()

			// Run the service with these parameters
			// on the application start
			.ScheduleOnce(new SomeEvent("Hello world! 1st run."))
			.ScheduleOnce(new SomeEvent[] {
				new SomeEvent("Hello world! 2st run."),
				new SomeEvent("Hello world! 3rd run.")
			})

			// Injects IJobTrigger<SomeEventService, SomeEvent> to DI
			// so you can pass data to the background worker
			// from your controller or services!
			.InjectTrigger()

			// Initializes RedisDataSource and listens for incoming job
			// you can add multiple datasources per background service!
			.AddDataSource<RedisDataSource>()

			.Build();
		);
	});

	// the rest of the configure function
}
```

## Inside a controller

```csharp
public class SomeController : Controller {
  private readonly IJobTrigger<SomeEventService, SomeEvent> someEventService;
  private readonly IJobTrigger<SomeParameterlessBackgroundService> helloWorldTrigger;

  public SomeController(
		IJobTrigger<SomeEventService, SomeEvent> someEventService,
		IJobTrigger<HelloWorldBackgroundService> helloWorldTrigger
  ) {
		this.helloWorldTrigger = helloWorldTrigger;
		this.someEventService = someEventService;
  }

  [HttpGet("/postjob")]
  public bool PostJob(string message) {
		SomeEventService.Run(new SomeEvent(message)); // it queues the event to run 
																									// on the background!
  }

  [HttpGet("/checkresults")]
  public bool PostJob(string message) {
		SomeParameterlessService.Run(); // it queues the event to run
																		// on the background!
  }
}
```

## License

© 2019 [Burak Tamturk](https://buraktamturk.org/)

Released under the [MIT LICENSE](http://opensource.org/licenses/MIT)
