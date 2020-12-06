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
```