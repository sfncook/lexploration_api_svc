using Azure.Storage.Queues;
using System;
using System.Text;
using System.Threading.Tasks;

public class QueueService
{
    private readonly QueueClient queueClient;

    public QueueService()
    {
        queueClient = new QueueClient(
            "DefaultEndpointsProtocol=https;AccountName=kelichatbot2;AccountKey=IgE1pLaUd5b+JOftL5wGogI1lnEQa0FoYK31yPeNwzcOeboqktRV7xntaEh8APmT+sXpX7niYIfC+AStHRnN3A==;EndpointSuffix=core.windows.net",
            "scrape-links"
        );
        queueClient.CreateIfNotExists();
    }

    public async Task EnqueueMessageAsync(string message)
    {
        Console.WriteLine(message);
        if (string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));

        var bytes = Encoding.UTF8.GetBytes(message);
        await queueClient.SendMessageAsync(Convert.ToBase64String(bytes));
    }
}

// Usage
//var queueService = new QueueService("YourConnectionString", "YourQueueName");
//await queueService.EnqueueMessageAsync("YourMessage");
