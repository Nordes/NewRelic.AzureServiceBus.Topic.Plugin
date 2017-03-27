using System.Collections.Generic;

namespace NewRelic.AzureServiceBus.Topic.Plugin.Tests
{
    internal static class TestHelper
    {
        /// <summary>
        /// Generate a basic configuration with nothing special.
        /// </summary>
        /// <returns></returns>
        internal static Models.SystemConfiguration GetBasicConfiguration()
        {
            var config = new Models.SystemConfiguration()
            {
                Name = "TestConfig",
                TopicSettings = new List<Models.ServiceBusTopicSettings>()
                {
                    new Models.ServiceBusTopicSettings() {
                        Name = "AccountTopicA",
                        ConnectionString = "fake",
                        Groups = null
                    }
                }
            };

            return config;
        }
    }
}
