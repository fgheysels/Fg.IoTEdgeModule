using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace Fg.IoTEdgeModule
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder ConfigureIoTEdgeModuleClient(this IHostBuilder builder,
                                                                TransportType transportType,
                                                                Action<ModuleClient> configureModuleClient = null)
        {
            var moduleClient = CreateModuleClient(transportType);

            if (configureModuleClient != null)
            {
                configureModuleClient(moduleClient);
            }

            builder.ConfigureServices(configure =>
            {
                configure.AddSingleton<ModuleClient>(_ => moduleClient);
            });

            return builder;
        }

        private static ModuleClient CreateModuleClient(TransportType transportType)
        {
            ITransportSettings settings;

            switch (transportType)
            {
                case TransportType.Amqp:
                case TransportType.Amqp_Tcp_Only:
                case TransportType.Amqp_WebSocket_Only:
                    settings = new AmqpTransportSettings(transportType);
                    break;

                case TransportType.Mqtt:
                case TransportType.Mqtt_Tcp_Only:
                case TransportType.Mqtt_WebSocket_Only:
                    settings = new MqttTransportSettings(transportType);
                    break;

                case TransportType.Http1:
                    settings = new Http1TransportSettings();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Open a connection to the Edge runtime
            var ioTHubModuleClient = ModuleClient.CreateFromEnvironmentAsync(new[] { settings }).GetAwaiter().GetResult();

            ioTHubModuleClient.OpenAsync().Wait();

            return ioTHubModuleClient;
        }
    }
}
