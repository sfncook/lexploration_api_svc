public class MySettings
{
    // Cosmos DB
    public string CosmosUri { get; set; }
    public string TableChatbots { get; set; }
    public string TableCompanies { get; set; }
    public string TableConversations { get; set; }
    public string TableLinks { get; set; }
    public string TableMessages { get; set; }
    public string TableRefinements { get; set; }
    public string TableUsers { get; set; }
    public string TableLogs { get; set; }
    public string TableMetrics { get; set; }

    // Queues
    public string QueueLinks { get; set; }
    public string QueueEmails { get; set; }
    public int LinkScrapeQueuePollRateMs { get; set; } = 10000;

    public string PineConeHost { get; set; }

    // Logging
    public bool PrintLogsStdOut { get; set; }
    public bool WriteLogsCosmos { get; set; }
    public int LogBufferPollRateMs { get; set; } = 10000;
    public string LogLevel { get; set; }

    // Metrics
    public bool WriteMetricsCosmos { get; set; }
    public int MetricsBufferPollRateMs { get; set; }

    // Hubspot
    public string QueueHubspotUpdate { get; set; }
    public int HubspotUpdateQueuePollRateMs { get; set; } = 10000;
}
