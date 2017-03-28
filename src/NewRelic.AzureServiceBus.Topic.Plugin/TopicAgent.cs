using System;
using NewRelic.Platform.Sdk;
using System.Linq;
using NewRelic.AzureServiceBus.Topic.Plugin.Models;
using System.Collections.Generic;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Dynamic;

namespace NewRelic.AzureServiceBus.Topic.Plugin
{
    /// <summary>
    /// Plugin Agent in order to monitor storage account.
    /// </summary>
    /// <remarks>
    /// One topic instance per different topic configuration(<see cref="https://github.com/newrelic-platform/newrelic_dotnet_sdk"/> and 
    /// <seealso cref="https://docs.newrelic.com/docs/plugins/developing-plugins/writing-code/using-net-sdk"/>)
    /// </remarks>
    public class TopicAgent : Agent
    {
        private Dictionary<string, int> messages = new Dictionary<string, int>(); // label + amount
        private SystemConfiguration _systemConfiguration;

        /// <summary>
        /// Plugin Guid
        /// </summary>
        /// <remarks>
        /// As proposed by NewRelic, if we are in debug we should use a different guid. Since we don't want that
        /// Guid to be in the App.Config, here's the trick.
        /// </remarks>
#if DEBUG
        public override string Guid => "NewRelic.AzureServiceBus.Topic.Plugin.Debug";
#else
        public override string Guid => "NewRelic.AzureServiceBus.Topic.Plugin";
#endif

        /// <summary>
        /// Return the assembly version
        /// </summary>
        public override string Version => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        /// <summary>
        /// Queue Agent
        /// </summary>
        /// <param name="systemConfiguration">The system configuration.</param>
        /// <param name="eventLogLogger">The event log logger.</param>
        public TopicAgent(SystemConfiguration systemConfiguration)
        {
            ValidateAgentConfiguration(systemConfiguration);
            _systemConfiguration = systemConfiguration;
            Initialize(systemConfiguration.TopicSettings);
        }

        private void Initialize(List<ServiceBusTopicSetting> topicSettings)
        {
            // Foreach topic configuration, create a new service listener.
            foreach (var topicSetting in topicSettings)
            {
                var namespaceManager = NamespaceManager.CreateFromConnectionString(topicSetting.ConnectionString);
                var Client = SubscriptionClient.CreateFromConnectionString(topicSetting.ConnectionString, topicSetting.Name, topicSetting.SubscriptionName, ReceiveMode.PeekLock);
                // messages.Add(Client);

                if (!namespaceManager.SubscriptionExists(topicSetting.Name, topicSetting.SubscriptionName.ToLower()))
                {
                    namespaceManager.CreateSubscription(topicSetting.Name, topicSetting.SubscriptionName.ToLower()); // Todo: Here we could add the filter
                }

                // var a = new Services.ServiceBusServices(settings.ConnectionString, settings.Name);
                OnMessageOptions options = new OnMessageOptions()
                {
                    AutoComplete = true,
                    AutoRenewTimeout = TimeSpan.FromMinutes(1)
                };

                Client.OnMessage((message) =>
                {
                    try
                    {
                        // If reporteveryhit 
                        // ReportMetric(message.Label, "messages", 1);

                        foreach (var propertyToGather in topicSetting.Properties)
                        {
                            // Metric name: $"Topics/{config.Name}/all/{topicSetting.Name}/{propertyToGather.Name}/{propertyToGather.AggregationType}";
                            if (propertyToGather.Name.Equals("label", StringComparison.OrdinalIgnoreCase))
                            {
                                // Most likely to happen and it's a hit counter
                                ReportMetric($"Topics/{_systemConfiguration.Name}/all/{topicSetting.Name}/{propertyToGather.Name}/{message.Label}", "hits", 1);
                            }
                            else
                            {
                                // It's a label, please do something
                                if (message.Properties.TryGetValue(propertyToGather.Name, out object result))
                                {
                                    if (float.TryParse(result.ToString(), out float floatResult))
                                    {
                                        ReportMetric(
                                            $"Topics/{_systemConfiguration.Name}/all/{topicSetting.Name}/{propertyToGather.Name}",
                                            propertyToGather.AggregationType.ToString(),
                                            floatResult);
                                    }
                                    else
                                    {
                                        ReportMetric(
                                            $"Topics/{_systemConfiguration.Name}/all/{topicSetting.Name}/{propertyToGather.Name}",
                                            propertyToGather.AggregationType.ToString(),
                                            0);
                                    }
                                }
                            }
                        }

                        // Process message from subscription.

                        ////Stream stream = message.GetBody<Stream>();

                        ////XmlDocument document = new XmlDocument();

                        ////using (XmlReader reader = XmlReader.Create(stream))
                        ////{
                        ////    document.Load(stream);
                        ////}

                        ////StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                        ////string s = reader.ReadToEnd();
                        ////XDocument doc = XDocument.Parse(s);
                        ////string jsonText = JsonConvert.SerializeXNode(doc);
                        ////dynamic dyn = JsonConvert.DeserializeObject<ExpandoObject>(jsonText);

                        // Console.WriteLine("Body: " + message.GetBody<dynamic>());
                        //Console.WriteLine("MessageID: " + message.MessageId);
                        //Console.WriteLine("Message Number: " + message.Properties["MessageNumber"]);

                        // var messageBodyType = message.Properties["messageType"].ToString();
                        // var contentType = Type.GetType(messageBodyType);
                        // var runtimeInvokableMethod = typeof(BrokeredMessage).GetMethod("GetBody", new Type[] { }).MakeGenericMethod(contentType);
                        //var messageBody = runtimeInvokableMethod.Invoke(message, null);
                        //var serviceBusEventInfo = new
                        //{
                        //    EventName = message.Label,
                        //    Content = JsonConvert.SerializeObject(messageBody),
                        //    Received = DateTime.Now,
                        //};

                        // Remove message from subscription.
                        message.Complete();
                    }
                    catch (Exception ex)
                    {
                        // Indicates a problem, unlock message in subscription.
                        message.Abandon();
                    }
                }, options);
            }
        }

        private void ValidateAgentConfiguration(SystemConfiguration systemConfiguration)
        {
            if (systemConfiguration == null)
                throw new ArgumentNullException(nameof(systemConfiguration), "The systemConfiguration must be specified for the agent to initialize");
            if (string.IsNullOrEmpty(systemConfiguration.Name))
                throw new ArgumentNullException(nameof(systemConfiguration.Name), "The system name must be specified for the agent to initialize");
            if (systemConfiguration.TopicSettings == null || !systemConfiguration.TopicSettings.Any())
                throw new ArgumentNullException(nameof(systemConfiguration.TopicSettings), "The system have no topic set and it must be specified for the agent to initialize");
            foreach (var topicSetting in systemConfiguration.TopicSettings)
            {
                if (string.IsNullOrEmpty(topicSetting.Name))
                    throw new ArgumentNullException(nameof(topicSetting.Name), "The name of the service bus topic must be specified for the agent to initialize");
                if (string.IsNullOrEmpty(topicSetting.ConnectionString))
                    throw new ArgumentNullException(nameof(topicSetting.ConnectionString), $"The connectionString of the Azure ServiceBus Topic \"{topicSetting.Name}\" must be specified for the agent to initialize");
                if (topicSetting.Groups != null)
                {
                    foreach (var group in topicSetting.Groups)
                    {
                        if (string.IsNullOrEmpty(group.Name))
                            throw new ArgumentNullException(nameof(group.Name), "The name of the group, if defined, must be specified for the agent to initialize");
                        if (string.IsNullOrEmpty(group.Regex))
                            throw new ArgumentNullException(nameof(group.Regex), $"The regex of the group {group.Name} must be specified for the agent to initialize");
                    }
                }
            }
        }

        /// <summary>
        /// Returns a human-readable string to differentiate different hosts/entities in the site UI
        /// </summary>
        /// <returns>The current system agent name</returns>
        public override string GetAgentName()
        {
            return _systemConfiguration.Name;
        }

        /// <summary>
        /// This is where logic for fetching and reporting metrics should exist.
        /// Call off to a REST head, SQL DB, virtually anything you can programmatically
        /// get metrics from and then call ReportMetric.
        /// </summary>
        public override void PollCycle()
        {
            // It will send what previously was ready to be sent. (for now)

            //foreach (var topicSetting in _systemConfiguration.TopicSettings)
            //{
            //    // var config = topicSetting;
            //}
        }
    }
}
