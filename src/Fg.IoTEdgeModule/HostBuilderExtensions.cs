using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using System;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting
{
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Configures and registers a <see cref="ModuleClient"/> instance as a Singleton.
        /// </summary>
        /// <remarks>The ModuleClient is used to interact with an IoT Edge module</remarks>
        /// <param name="builder"></param>
        /// <param name="transportType">The protocol that must be used by the moduleclient</param>
        /// <param name="configureModuleClient">An action that describes how the ModuleClient must be configured.</param>
        /// <returns></returns>
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
