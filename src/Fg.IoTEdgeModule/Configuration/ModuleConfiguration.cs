using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Fg.IoTEdgeModule.Configuration
{
    public abstract class ModuleConfiguration
    {
        protected ILogger Logger;

        protected ModuleConfiguration(ILogger logger)
        {
            Logger = logger;
        }

        protected abstract string ModuleName { get; }

        public static async Task<TModuleConfiguration> CreateFromTwinAsync<TModuleConfiguration>(ModuleClient moduleClient, ILogger logger) where TModuleConfiguration : ModuleConfiguration
        {
            var config = 
                (TModuleConfiguration)Activator.CreateInstance(typeof(TModuleConfiguration),
                                                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
                                                        null, new object[] { logger }, null);

            var moduleTwin = await moduleClient.GetTwinAsync();

            config.InitializeFromTwin(moduleTwin.Properties.Desired);

            await config.UpdateReportedPropertiesAsync(moduleClient);

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

        protected abstract void InitializeFromTwin(TwinCollection desiredProperties);

        /// <summary>
        /// Set the Module's specific Configuration settings in the Module Twin.
        /// </summary>
        /// <param name="reportedProperties">The TwinCollection that must be used to set the reported properties.</param>
        protected abstract void SetReportedProperties(TwinCollection reportedProperties);
    }
}