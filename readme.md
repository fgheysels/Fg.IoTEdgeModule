# Fg.IoTEdgeModule

[![Build Status](https://frederikgheysels.visualstudio.com/GitHub%20Pipelines/_apis/build/status/IoTEdgeModule/Fg.IoTEdgeModule%20CI?branchName=main)](https://frederikgheysels.visualstudio.com/GitHub%20Pipelines/_build/latest?definitionId=9&branchName=main)
[![NuGet Badge](https://buildstats.info/nuget/fg.iotedgemodule?includePreReleases=true)](https://www.nuget.org/packages/Fg.IoTEdgeModule)

## Introduction

This project provides some helpful functionality to easily create better modules for Azure IoT Edge.

## Installation

```
PM > Install-Package Fg.IoTEdgeModule
```

## Creating IoT Edge modules as a hosted service

When creating a new Azure IoT Edge module in Visual Studio, the VS.NET template generates a straightforward console application.  If you want to make use of dependency injection, easy integration of `ILogger`, and have a better distinction between infrastructure and the application functionality itself, it's better to setup the module as a hosted module.

The `CreateModuleClient` extension method on `IHostBuilder` allows to easily create, configure and register the `ModuleClient` as a singleton service in the host.
Using this method allows you to easily setup your IoT Edge module as a hosted service:

```csharp
static async Task Main(string[] args)
{
    var host = Host.CreateDefaultBuilder(args)
                    .ConfigureIoTEdgeModuleClient(TransportType.Mqtt_Tcp_Only, configureModuleClient =>
                    {
                        configureModuleClient.OpenAsync().Wait();
                        configureModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesChanged, configureModuleClient).Wait();
                    })
                    .ConfigureLogging(logging => logging.AddConsole(consoleLogging =>
                    {
                        consoleLogging.Format = ConsoleLoggerFormat.Systemd;
                        consoleLogging.TimestampFormat = "dd/MM/yyyy HH:mm:ss zz";
                    }))
                    .ConfigureServices(services =>
                    {
                        // Add other dependencies to the DI container
                        // ...

                        // 
                        services.AddHostedService<App>();
                    })
                    .UseConsoleLifetime()
                    .Build();

    await host.RunAsync();
}

private static OnDesiredPropertiesChanged(TwinCollection desiredProperties, object userContext) {}
```

The actual IoT Edge Module's functionality is in this example implemented in the `App` class, which looks like this:

```csharp
internal class App : BackgroundService
{

    private readonly ModuleClient _iotHubModuleClient;
    private readonly ILogger<App> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App(ModuleClient iotHubModuleClient, ILogger<App> logger)
    {
        _iotHubModuleClient = iotHubModuleClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while( !cancellationToken.IsCancellationRequested )
        {
            // Do some work here
        }
    }

    public  Task StopAsync(CancellationToken cancellationToken)
    {
        // TODO: clean up
        return Task.CompletedTask;
    }
}
```

## Graceful shutdown of IoT Edge modules

When the IoT Edge runtime restarts an IoT Edge module (container), it seems that the running container instance is just killed. To be able to gracefully shutdown the module, it is required that the module is notified when a shutdown is happening.
To be able to do this, the `ShutdownHandler` class has been introduced.  (This class is taken from the [`EdgeUtil`](https://github.com/Azure/iotedge/issues/5274#issuecomment-885965160) codebase is a little bit modified).

Creating an instance of the `ShutdownHandler` class offers you a `CancellationTokenSource` that is tied to the shutdown process of the running container.  In other words: when the container is being termined, the `CancellationTokenSource` is being canceled.
This means that the `CancellationToken` that is linked to it can be used in the module to determine if the module is being shut down:

```csharp
var shutdownHandler = ShutdownHandler.Create(shutdownWaitPeriod: TimeSpan.FromSeconds(5), logger: log);

while( !shutdownHandler.CancellationTokenSource.Token.IsCancellationRequested )
{
  // do work
}
```

The `ShutdownHandler` also offers a mechanism to make sure that everything can be cleaned up before completely shutting down the container.  The shutdown process will wait until the `SignalCleanupComplete()` method is called or until the `shutdownWaitPeriod` has been elapsed.

```csharp
var shutdownHandler = ShutdownHandler.Create(shutdownWaitPeriod: TimeSpan.FromSeconds(5), logger: log);

while( !shutdownHandler.CancellationTokenSource.Token.IsCancellationRequested )
{
  // do work
}

// Cleanup / Dispose some things
dbConnection.Close();
moduleClient.Dispose();

shutdownHandler.SignalCleanupComplete();
```

### Using ShutdownHandler with IHost

To use the `ShutdownHandler` in combination with `HostBuilder`/`IHost`, the following approach is adivsed:

```csharp
using( var host = CreateHostBuilder().Build())
{
    var logger = host.Services.GetService<ILoggerFactory>().GetLogger<Program>()
    var shutdownHandler = ShutdownHandler.Create(TimeSpan.FromSeconds(20), logger)

    await host.StartAsync(shutdownHandler.CancellationTokenSource.Token);
    logger.LogInformation("Module stopping ... ");
    await host.WaitForShutdownAsync(shutdownHandler.CancellationTokenSource.Token);

    logger.LogInformation("Module stopped");

    shutdownHandler.SignalCleanupComplete();
}
```

Note that in the above code snippet we do not use `RunAsync`, but explicitly call `StartAsync` and `WaitForShutdownAsync`.  This is a [workaround](https://github.com/dotnet/runtime/issues/44086#issuecomment-811126003) for [this](https://github.com/dotnet/runtime/issues/44086) issue.

### ModuleConfiguration

This library contains an abstract `ModuleConfiguration` class that allows you to abstract configuration-settings for an IoT Edge module.
The `ModuleConfiguration` class allows you to easily retrieve the desired properties from the module-twin and update the reported properties to the module twin as well.

To use this functionality, you need to inherit from this base-class and implement some basic functionality:

```csharp
public class MyModuleConfiguration : ModuleConfiguration
{
    public int SomeIntegerProperty {get; private set;}
    public string SomeStringProperty {get; private set;}

    protected override void InitializeFromTwin(TwinCollection desiredProperties)
    {
        SomeIntegerProperty = Convert.ToInt32(desiredProperties["SomeIntegerConfigSetting"]);
        SomeStringProperty = Convert.ToString(desiredProperties["SomeStringConfigSetting"]);
    }

    protected override void SetReportedProperties(TwinCollection reportedProperties)
    {
        reportedProperties["SomeIntegerConfigSetting"] = SomeIntegerProperty;
        reportedProperties["SomeStringConfigSetting"] = SomeStringProperty;
    }
}
```

Usage:

```csharp
var configuration = await ModuleConfiguration.CreateFromTwinAsync<MyModuleConfiguration>(moduleClient, logger);

// Use the settings that originate from the ModuleTwin in some other classes.
var processor = new MyProcessor(configuration.SomeIntegerProperty);
```


