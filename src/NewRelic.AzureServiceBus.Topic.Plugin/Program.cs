using Topshelf;

namespace NewRelic.AzureServiceBus.Topic.Plugin
{
    /// <summary>
    /// Plugin application
    /// </summary>
    class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <remarks>To install as a service, just use "app.exe install"</remarks>
        public static void Main()
        {
            HostFactory.Run(x =>
            {
                x.SetDescription("NewRelic Windows Azure ServiceBus Topic plugin");
                x.SetDisplayName("NewRelic Azure ServiceBus Topic");
                x.SetServiceName("NewRelic.AzureServiceBus.Topic.Plugin");

                x.RunAsLocalSystem();
                x.StartAutomatically();
                var svc = x.Service<AgentManager>();

                x.OnException(e =>
                {
                    System.Diagnostics.Trace.TraceError(e.Message + System.Environment.NewLine + "StackTrace: " + System.Environment.NewLine + e.StackTrace);
                });

                x.EnableServiceRecovery(rc =>
                {
                    rc.RestartService(1); // restart the service after 1 minute
                    rc.SetResetPeriod(1); // set the reset interval to one day
                });
            });
        }
    }
}
