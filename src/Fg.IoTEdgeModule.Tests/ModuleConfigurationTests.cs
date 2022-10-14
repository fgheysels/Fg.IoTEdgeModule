using System.Security.Cryptography.X509Certificates;
using Fg.IoTEdgeModule.Configuration;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fg.IoTEdgeModule.Tests
{
    public class ModuleConfigurationTests : IClassFixture<EnvironmentFixture>
    {
        public ModuleConfigurationTests(EnvironmentFixture environmentFixture)
        {
                
        }

        [Fact]
        public void CanCreateConfiguration()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(Environment.GetEnvironmentVariable("EDGEMODULE_CONNECTIONSTRING"));

            var config
                = ModuleConfiguration.CreateFromTwinAsync<TestModuleConfiguration>(moduleClient, NullLogger.Instance);

            Assert.NotNull(config);
        }

        private class TestModuleConfiguration : ModuleConfiguration
        {
           public TestModuleConfiguration(ILogger logger):base(logger){}
            protected override string ModuleName => "TestModule";
            protected override void InitializeFromTwin(TwinCollection desiredProperties)
            {
                int x = desiredProperties.Count;
            }

            protected override void SetReportedProperties(TwinCollection reportedProperties)
            {
             
            }
        }
    }
}