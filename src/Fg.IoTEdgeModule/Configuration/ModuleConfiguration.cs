using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Fg.IoTEdgeModule.Configuration
{
    public abstract class ModuleConfiguration
    {
        protected ILogger<ModuleConfiguration> Logger;

        protected ModuleConfiguration(ILogger<ModuleConfiguration> logger)
        {
            Logger = logger;
        }

        protected abstract string ModuleName { get; }

        // TODO: find a way to 'force' inheritors to create / override a factory method for this type.
        //       The factory method should read values from the Module Twin, and set them in the configuration.
        
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
                Logger.LogError($"An error occured while trying to update the Module Twin's reported properties: {ex.Message}");
            }
        }

        /// <summary>
        /// Set the Module's specific Configuration settings in the Module Twin.
        /// </summary>
        /// <param name="reportedProperties">The TwinCollection that must be used to set the reported properties.</param>
        protected abstract void SetReportedProperties(TwinCollection reportedProperties);
    }
}