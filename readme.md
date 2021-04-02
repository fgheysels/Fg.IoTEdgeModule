# Fg.IoTEdgeModule

[![Build Status](https://frederikgheysels.visualstudio.com/GitHub%20Pipelines/_apis/build/status/IoTEdgeModule/Fg.IoTEdgeModule%20CI?branchName=main)](https://frederikgheysels.visualstudio.com/GitHub%20Pipelines/_build/latest?definitionId=9&branchName=main)
[![NuGet Badge](https://buildstats.info/nuget/fg.iotedgemodule?includePreReleases=true)](https://www.nuget.org/packages/Fg.IoTEdgeModule)

## Introduction

This project provides some helpful functionality to easily create better modules for Azure IoT Edge.

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