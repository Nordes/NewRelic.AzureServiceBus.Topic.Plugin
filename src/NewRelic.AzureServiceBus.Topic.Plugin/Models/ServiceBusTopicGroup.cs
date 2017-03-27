using System.Text.RegularExpressions;

namespace NewRelic.AzureServiceBus.Topic.Plugin.Models
{
    public class ServiceBusTopicGroup
    {
        public string Name { get; set; }
        public string Regex { get; set; }

        public bool AllowedInGroup(string queueName)
        {
            return new Regex(Regex, RegexOptions.CultureInvariant).IsMatch(queueName);
        }
    }
}
