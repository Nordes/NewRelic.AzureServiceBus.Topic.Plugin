namespace NewRelic.AzureServiceBus.Topic.Plugin.Models
{
    public enum PropertyAggregationType
    {
        /// <summary>
        /// Hit count of that property if it exists
        /// </summary>
        /// <remarks>
        /// Can do a count over field having a string.
        /// </remarks>
        /// <example>
        /// How many hit on Label having HighPriority in a period of 60 seconds (default)
        /// </example>
        Count,

        /// <summary>
        /// Average of the value between all the messages received
        /// </summary>
        /// <example>
        /// How much average value is located in a property in a period of 60 seconds (default)
        /// </example>
        Avg,

        /// <summary>
        /// Sum of the value between all the messages received
        /// </summary>
        /// <example>
        /// How many aggregated value is located in a property in a period of 60 seconds (default)
        /// </example>
        Sum,

        /// <summary>
        /// In combinaison with send at every request. Will send the value itself.
        /// </summary>
        Value
    }
}
