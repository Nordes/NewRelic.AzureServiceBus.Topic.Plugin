using System;
using NewRelic.Platform.Sdk;
using System.Linq;
using NewRelic.AzureServiceBus.Topic.Plugin.Models;
using System.Collections.Generic;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;
using Newtonsoft.Json;

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
        private List<Object> messages = new List<object>();
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

        private void Initialize(List<ServiceBusTopicSettings> topicSettings)
        {
            // Foreach topic configuration, create a new service listener.
            foreach (var topicSetting in topicSettings)
            {
                var namespaceManager = NamespaceManager.CreateFromConnectionString(topicSetting.ConnectionString);
                var Client = SubscriptionClient.CreateFromConnectionString(topicSetting.ConnectionString, topicSetting.Name, topicSetting.SubscriptionName, ReceiveMode.ReceiveAndDelete);
                messages.Add(Client);

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
                        ReportMetric(message.Label, "messages", 1);
                        // Process message from subscription.
                        //Console.WriteLine("Body: " + message.GetBody<string>());
                        //Console.WriteLine("MessageID: " + message.MessageId);
                        //Console.WriteLine("Message Number: " + message.Properties["MessageNumber"]);

                        //var messageBodyType = message.Properties["messageType"].ToString();
                        //var contentType = Type.GetType(messageBodyType);
                        //var runtimeInvokableMethod = typeof(BrokeredMessage).GetMethod("GetBody", new Type[] { }).MakeGenericMethod(contentType);
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
            foreach (var topicSetting in _systemConfiguration.TopicSettings)
            {
                // var config = topicSetting;
                SendTopicStats();
            }
        }

        /// <summary>
        /// Polls the storage account asynchronous.
        /// </summary>
        /// <param name="config">The configuration.</param>
        private void SendTopicStats()
        {
            //var storageAccount = CloudStorageAccount.Parse(config.ConnectionString);
            //var queueClient = storageAccount.CreateCloudQueueClient();
            //var continuationToken = new QueueContinuationToken();

            //while (continuationToken != null)
            //{
            //    var listResponse = queueClient.ListQueuesSegmented(continuationToken);

            //    // We must ask Azure for the size of each queue individually.
            //    // This can be done in parallel.
            //    foreach (var queue in listResponse.Results)
            //    {
            //        queue.FetchAttributes();
            //    }

            //    // ReportMetric is not thread-safe, so we can't call it in the parallel
            //    foreach (var topic in listResponse.Results)
            //    {
            //        var approximateMessageCount = topic.ApproximateMessageCount ?? 0;

            //        // No groups, then just send and continue.
            //        var metricName = $"Topics/{config.Name}/all/{topic.Name}/size";
            //        ReportMetric(metricName, "messages", approximateMessageCount);

            //        if (config.Groups != null)
            //        {
            //            // Send the data to the proper group.
            //            foreach (var topicGroup in config.Groups)
            //            {
            //                if (topicGroup.AllowedInGroup(topic.Name))
            //                {
            //                    metricName = $"Queues/{config.Name}/groups/{topicGroup.Name}/{topic.Name}/size";
            //                    ReportMetric(metricName, "messages", approximateMessageCount);
            //                }
            //            }
            //        }
            //    }

            //    continuationToken = listResponse.ContinuationToken;
            //}
        }
    }
}
