using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Fg.IoTEdgeModule.Configuration
{
    /// <summary>
    /// Base class for representing an IoT Edge Module's configuration.
    /// </summary>
    public abstract class ModuleConfiguration
    {
        protected ILogger Logger;

        protected ModuleConfiguration(ILogger logger)
        {
            Logger = logger;
        }

        protected abstract string ModuleName { get; }

        /// <summary>
        /// Instantiate a <paramref name="TModuleConfiguration"/> instance and populate the configuration properties
        /// with values that are defined in the Module Twin.
        /// </summary>
        public static async Task<TModuleConfiguration> CreateFromTwinAsync<TModuleConfiguration>(ModuleClient moduleClient, ILogger logger) where TModuleConfiguration : ModuleConfiguration
        {
            var config =
                (TModuleConfiguration)Activator.CreateInstance(
                    typeof(TModuleConfiguration),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, 
                    new object[] { logger }, 
                    null);

            var moduleTwin = await moduleClient.GetTwinAsync();

            config.InitializeFromTwin(moduleTwin.Properties.Desired);

            await config.UpdateReportedPropertiesAsync(moduleClient);

            return config;
        }

        /// <summary>
        /// Instantiate a <paramref name="TModuleConfiguration"/> instance and populate the configuration properties
        /// with values that are present in the <paramref name="desiredProperties"/> twin-collection.
        /// </summary>
        /// <remarks>Use this method for testing your <see cref="ModuleConfiguration"/> implementation.</remarks>
        public static TModuleConfiguration CreateFromTwin<TModuleConfiguration>(TwinCollection desiredProperties, ILogger logger) where TModuleConfiguration : ModuleConfiguration
        {
            var config =
                (TModuleConfiguration)Activator.CreateInstance(
                    typeof(TModuleConfiguration),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new object[] { logger },
                    null);

            config.InitializeFromTwin(desiredProperties);

            return config;
        }

        /// <summary>
        /// Reports the configuration settings via the Module Twin reported properties.
        /// </summary>
        /// <param name="moduleClient">The ModuleClient that must be used to communicate.</param>
        /// <returns></returns>
        public async Task UpdateReportedPropertiesAsync(ModuleClient moduleClient)
        {
            var reportedProperties = new TwinCollection();

            SetReportedProperties(reportedProperties);

            try
            {
                await moduleClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                Logger.LogError($"An error occurred while trying to update the Module Twin's reported properties: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the Module's specific Configuration settings from the desiredProperties TwinCollection of the module Twin.
        /// </summary>
        /// <param name="desiredProperties">The TwinCollection that contains the desired properties from the Module Twin.</param>
        protected abstract void InitializeFromTwin(TwinCollection desiredProperties);

        /// <summary>
        /// Set the Module's specific Configuration settings in the Module Twin.
        /// </summary>
        /// <param name="reportedProperties">The TwinCollection that must be used to set the reported properties.</param>
        protected abstract void SetReportedProperties(TwinCollection reportedProperties);
    }
}