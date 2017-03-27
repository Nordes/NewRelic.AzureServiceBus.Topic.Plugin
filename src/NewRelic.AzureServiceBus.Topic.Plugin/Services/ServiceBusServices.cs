using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Environment = System.Environment;
using Microsoft.Azure;

namespace NewRelic.AzureServiceBus.Topic.Plugin.Services
{
    internal class ServiceBusServices
    {
        private readonly string TopicName = string.Empty;
        private readonly string _subscriptionName = Environment.MachineName;
        private readonly string _connectionString;
        private readonly NamespaceManager _namespaceManager;
        private readonly TopicClient _topicClient;

        public ServiceBusServices(string connectionString, string topicName)
        {
            _connectionString = connectionString;
            _namespaceManager = NamespaceManager.CreateFromConnectionString(_connectionString);
            _topicClient = TopicClient.CreateFromConnectionString(_connectionString, TopicName);
            Configure();
        }

        private void Configure()
        {
            TopicDescription topicDescription = new TopicDescription(TopicName)
            {
                DefaultMessageTimeToLive = TimeSpan.FromMinutes(10)
            };

            // Setup the topic
            if (!_namespaceManager.TopicExists(TopicName))
            {
                // Should never arrive here...
                _namespaceManager.CreateTopic(topicDescription);
            }

            // Setup the subscription
            if (!_namespaceManager.SubscriptionExists(TopicName, _subscriptionName))
            {
                //TopicDescription td = new TopicDescription(TopicName)
                //{
                //    DefaultMessageTimeToLive = TimeSpan.FromMinutes(5),
                //    AutoDeleteOnIdle = TimeSpan.FromHours(1),
                //    EnableBatchedOperations = true
                //};
                SubscriptionDescription subscriptionDescription = new SubscriptionDescription(TopicName, _subscriptionName)
                {
                    DefaultMessageTimeToLive = TimeSpan.FromMinutes(10),
                    AutoDeleteOnIdle = TimeSpan.FromDays(1)
                };

                _namespaceManager.CreateSubscription(subscriptionDescription);
            }
            
            // Filter can be created... but for now we don't
            // // Create a "HighMessages" filtered subscription.
            // SqlFilter highMessagesFilter = new SqlFilter("MessageNumber > 3");
            // namespaceManager.CreateSubscription("TestTopic", "HighMessages", highMessagesFilter);

            // Send a message saying I exists.
            // TopicClient _topicClient = TopicClient.CreateFromConnectionString(connectionString, "TestTopic");
            // _topicClient.Send(new BrokeredMessage());
        }

        internal void PreStartListening()
        {
            //MessagingFactory msgFactory = MessagingFactory.CreateFromConnectionString(_connectionString);

            // var topicStuff = msgFactory.CreateTopicClient(TopicName);
            var topicStuff = TopicClient.CreateFromConnectionString(_connectionString, TopicName);
            var messageReceiver = topicStuff.MessagingFactory.CreateMessageReceiver(TopicName + "/subscriptions/" + _subscriptionName, ReceiveMode.PeekLock);
            
            // MessageReceiver messageReceiver = msgFactory.CreateMessageReceiver(TopicName, ReceiveMode.ReceiveAndDelete);
            

            while (messageReceiver.Peek() != null)
            {
                // Batch the receive operation
                var brokeredMessages = messageReceiver.ReceiveBatch(300);

                // Complete the messages
                var completeTasks = brokeredMessages.Select(m => Task.Run(() => m.Complete())).ToArray();

                // Wait for the tasks to complete. 
                // Task.WaitAll(completeTasks);
            }

            messageReceiver.Close();
        }

        /// <summary>
        /// Starts the listening. / Should be by environment....
        /// </summary>
        /// <param name="serviceBusEvenInfoList">The service bus even information list.</param>
        /// <param name="taskBarIcon">The task bar icon.</param>
        internal void StartListening(TrulyObservableCollection<ServiceBusEventInfo> serviceBusEvenInfoList, TaskbarIcon taskBarIcon)
        {
            //PreStartListening(); // TODO ... have the topic cleaned

            SubscriptionClient subscriptionClient = SubscriptionClient.CreateFromConnectionString(_connectionString, TopicName, _subscriptionName);
            subscriptionClient.PrefetchCount = 30;
            
            // Configure the callback options.
            OnMessageOptions options = new OnMessageOptions
            {
                AutoComplete = false,
                AutoRenewTimeout = TimeSpan.FromMinutes(1)
            };

            subscriptionClient.OnMessage((message) =>
            { 
                try
                {
                    var messageBodyType = message.Properties["messageType"].ToString();
                    var contentType = Type.GetType(messageBodyType);
                    var runtimeInvokableMethod = typeof(BrokeredMessage).GetMethod("GetBody", new Type[] { }).MakeGenericMethod(contentType);
                    var messageBody = runtimeInvokableMethod.Invoke(message, null);
                    var serviceBusEventInfo = new ServiceBusEventInfo
                    {
                        EventName = message.Label,
                        Content = JsonConvert.SerializeObject(messageBody),
                        Received = DateTime.Now,
                    };

                    if (messageBody is DataValidatedMessage)
                    {
                        var body = messageBody as DataValidatedMessage;
                        taskBarIcon.ShowBalloonTip("Publication", "Publication " + body.CustomerKey + " > " + body.ProjectKey + " Completed", BalloonIcon.Info);
                    }
                    else if (messageBody is StatusPingBackMessage)
                    {
                        // Process message from subscription.
                        var body = messageBody as StatusPingBackMessage;
                        serviceBusEventInfo.RefreshedOn = body.RefreshedOn;
                        serviceBusEventInfo.ServerName = body.WebSiteSiteName;

                        //Trace.TraceInformation("Body: " + message.GetBody<string>());
                        //Trace.TraceInformation("MessageID: " + message.MessageId);
                        //Trace.TraceInformation("Message Number: " + message.Properties["MessageNumber"]);
                    }

                    if (messageBody is BaseMessage)
                    {
                        var body = messageBody as BaseMessage;
                        serviceBusEventInfo.SolutionName = body.CustomerKey + "-" + body.ProjectKey;
                    }
                    

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        serviceBusEvenInfoList.Add(serviceBusEventInfo);
                    });

                    // Remove message from subscription.
                    message.Complete();
                }
                catch (Exception)
                {
                    // Indicates a problem, unlock message in subscription.
                    message.Abandon();
                }
            }, options);
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageBody">The message body.</param>
        internal void SendMessage<T>(T messageBody)
        {
            var messageType = typeof(T).Name;

            var message = new BrokeredMessage(messageBody) { Label = messageType };
            message.Properties["messageType"] = messageBody.GetType().AssemblyQualifiedName;
            _topicClient.Send(message);
        }

        //internal void CleanAllSubscription()
        //{
        //    var allSubscriptions = _namespaceManager.GetSubscriptions(TopicName);

        //    var subscriptionDescriptions = allSubscriptions as SubscriptionDescription[] ?? allSubscriptions.ToArray();
        //    for (var i = 0; i < subscriptionDescriptions.Count(); i++)
        //    {
        //        _namespaceManager.DeleteSubscription(subscriptionDescriptions[i].TopicPath, subscriptionDescriptions[i].Name);
        //    }
        //}

        /// <summary>
        /// Cleans the topic subscriptions.
        /// </summary>
        /// <param name="statusBarUpdateFnc">The status bar update FNC.</param>
        /// <param name="minMessage">The minimum amount of message in order to keep the subscription.</param>
        /// <returns></returns>
        internal async Task CleanSubscriptions(Action<string> statusBarUpdateFnc, int minMessage)
        {
            var subscriptions = _namespaceManager.GetSubscriptions(TopicName);

            // do stuff with subscriptions
            foreach (var subscription in subscriptions)
            {
                if (minMessage < subscription.MessageCount)
                {
                    statusBarUpdateFnc.Invoke(string.Format("Azure subscription: Removing {0} having more than {1} messages", subscription.Name, minMessage));
                    await _namespaceManager.DeleteSubscriptionAsync(TopicName, subscription.Name);
                }
            }
        }
    }
}
