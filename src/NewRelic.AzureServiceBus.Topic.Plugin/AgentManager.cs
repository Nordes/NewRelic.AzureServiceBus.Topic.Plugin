using System.Threading;
using NewRelic.Platform.Sdk;
using Topshelf;

namespace NewRelic.AzureServiceBus.Topic.Plugin
{
    /// <summary>
    /// NewRelic Agent manager
    /// </summary>
    class AgentManager: ServiceControl
    {
        private Thread _pollThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentManager"/> class.
        /// </summary>
        /// <param name="eventLogLogger">The event log logger.</param>
        public AgentManager()
        {
        }

        /// <summary>
        /// Start the Runner. NewRelic Runner is the main entrypoint class for the SDK. Essentially you will create an instance of a Runner, 
        /// assign either your programmatically configured Agents or your AgentFactory to it, and then call SetupAndRun() 
        /// which will begin invoking the PollCycle() method on each of your Agents once per polling interval.
        /// </summary>
        public bool Start(HostControl hostControl)
        {
            _pollThread = new Thread(() =>
            {
                var runner = new Runner();

                runner.Add(new TopicAgentFactory());

                runner.SetupAndRun();
            });

            _pollThread.Start();

            return true;
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public bool Stop(HostControl hostControl)
        {
            _pollThread.Abort();
            hostControl.Stop();

            return true;
        }
    }
}
