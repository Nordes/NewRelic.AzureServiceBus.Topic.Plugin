using System.Collections.Generic;

namespace NewRelic.AzureServiceBus.Topic.Plugin.Models
{
    public class ServiceBusTopicSettings
    {
        public string Name { get; set; }
        public string SubscriptionName { get; set; }
        public string ConnectionString { get; set; }
        public List<ServiceBusTopicGroup> Groups { get; set; }
    }
}
