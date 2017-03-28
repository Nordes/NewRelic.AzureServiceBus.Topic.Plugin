using System;

namespace NewRelic.AzureServiceBus.Topic.Plugin.Models
{
    public class PropertyWatchSetting
    {
        public string Name { get; set; }
        public PropertyAggregationType AggregationType { get; set; }
    }
}
