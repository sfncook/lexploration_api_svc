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

    // Queues
    public string QueueLinks { get; set; }
    public string QueueEmails { get; set; }

    public string PineConeHost { get; set; }
}
