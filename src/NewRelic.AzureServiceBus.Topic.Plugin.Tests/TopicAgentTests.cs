using System;
using NUnit.Framework;

namespace NewRelic.AzureServiceBus.Topic.Plugin.Tests
{
    [TestFixture(Category ="Agent")]
    public class TopicAgentTests
    {
        [Test(Description = "Create a basic agent with all required information")]
        public void ServiceBusTopicAgentConfiguration_Init_Ok()
        {
            // Arrange
            var agentConfig = TestHelper.GetBasicConfiguration();

            var agent = new TopicAgent(agentConfig);

            Assert.IsNotNull(agent);
        }

        [Test(Description = "Validate that the System name must be set. If it is empty, it will throw an argument null exception")]
        public void ServiceBusTopicAgentConfiguration_Init_InvalidSystemName()
        {
            // Arrange
            var agentConfig = TestHelper.GetBasicConfiguration();
            agentConfig.Name = null;

            Assert.Throws<ArgumentNullException>(() =>
            {
                var agent = new TopicAgent(agentConfig);
            });
        }

        [Test(Description = "The agent needs to have a name otherwise it cannot work")]
        public void GetAgentName_Ok()
        {
            // Arrange
            var agentConfig = TestHelper.GetBasicConfiguration();
            agentConfig.Name = "MyAgentNameTest";

            // Execute
            var agent = new TopicAgent(agentConfig);

            // Validate
            Assert.IsNotNull(agent.GetAgentName());
            Assert.AreEqual(agentConfig.Name, agent.GetAgentName());
        }

        [Test(Description = "The agent needs to have a version in order to be a valid \"plugin\"")]
        public void GetVersion_Ok()
        {
            // Arrange
            var agentConfig = TestHelper.GetBasicConfiguration();

            // Execute
            var agent = new TopicAgent(agentConfig);

            // Validate
            Assert.IsNotNull(agent.Version);
        }

        [Test(Description ="Validate that the GUID is set properly. In debug it will differ from release")]
        public void Guid_Ok()
        {
            // Arrange
            var agentConfig = TestHelper.GetBasicConfiguration();

            // Execute
            var agent = new TopicAgent(agentConfig);

            // Validate
            Assert.IsNotNull(agent.Guid);
#if DEBUG
            Assert.AreEqual(agent.Guid, "NewRelic.AzureServiceBus.Topic.Plugin.Debug");
#else
            Assert.AreEqual(agent.Guid, "NewRelic.AzureServiceBus.Topic.Plugin");
#endif
        }
    }
}
