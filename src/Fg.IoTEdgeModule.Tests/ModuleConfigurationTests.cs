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

        [Fact]
        public void CanInitializeConfiguration()
        {
            var desiredProperties = new TwinCollection("""{"SomeIntProperty":42, "SomeStringProperty": "stringvalue"}""");

            var config = ModuleConfiguration.CreateFromTwin<TestModuleConfiguration>(desiredProperties, NullLogger.Instance);

            Assert.Equal(42, config.SomeIntProperty);
            Assert.Equal("stringvalue", config.SomeStringProperty);
        }

        private class TestModuleConfiguration : ModuleConfiguration
        {
            public TestModuleConfiguration(ILogger logger) : base(logger) { }

            protected override string ModuleName => "TestModule";
           
            public int SomeIntProperty { get; private set; }
            public string SomeStringProperty { get; private set; }
            
            protected override void InitializeFromTwin(TwinCollection desiredProperties)
            {
                SomeIntProperty = desiredProperties["SomeIntProperty"];
                SomeStringProperty = desiredProperties["SomeStringProperty"];
            }

            protected override void SetReportedProperties(TwinCollection reportedProperties)
            {

            }
        }
    }
}