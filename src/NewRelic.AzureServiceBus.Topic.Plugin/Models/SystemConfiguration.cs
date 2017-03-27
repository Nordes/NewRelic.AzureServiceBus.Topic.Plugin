using System.Collections.Generic;

namespace NewRelic.AzureServiceBus.Topic.Plugin.Models
{
    public class SystemConfiguration
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public List<ServiceBusTopicSettings> TopicSettings { get; set; }
    }
}
