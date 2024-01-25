using Azure.Storage.Queues;
using Newtonsoft.Json;
using SalesBotApi.Models;
using System;
using System.Text;
using System.Threading.Tasks;

public class QueueService
{
    private readonly QueueClient scrapeLinksQueueClient;
    private readonly QueueClient sendEmailQueueClient;

    public QueueService()
    {
        scrapeLinksQueueClient = new QueueClient(
            "DefaultEndpointsProtocol=https;AccountName=kelichatbot2;AccountKey=IgE1pLaUd5b+JOftL5wGogI1lnEQa0FoYK31yPeNwzcOeboqktRV7xntaEh8APmT+sXpX7niYIfC+AStHRnN3A==;EndpointSuffix=core.windows.net",
            "scrape-links"
        );
        scrapeLinksQueueClient.CreateIfNotExists();

        sendEmailQueueClient = new QueueClient(
            "DefaultEndpointsProtocol=https;AccountName=kelichatbot2;AccountKey=IgE1pLaUd5b+JOftL5wGogI1lnEQa0FoYK31yPeNwzcOeboqktRV7xntaEh8APmT+sXpX7niYIfC+AStHRnN3A==;EndpointSuffix=core.windows.net",
            "send-email"
        );
        sendEmailQueueClient.CreateIfNotExists();
    }

    public async Task EnqueueScrapLinksMessageAsync(string message)
    {
        Console.WriteLine(message);
        if (string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));

        var bytes = Encoding.UTF8.GetBytes(message);
        await scrapeLinksQueueClient.SendMessageAsync(Convert.ToBase64String(bytes));
    }

    public async Task EnqueueSendEmailMessageAsync(EmailRequest emailRequest)
    {
        string message = JsonConvert.SerializeObject(emailRequest);
        Console.WriteLine(message);
        if (string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));

        var bytes = Encoding.UTF8.GetBytes(message);
        await sendEmailQueueClient.SendMessageAsync(Convert.ToBase64String(bytes));
    }
}

// Usage
//var queueService = new QueueService("YourConnectionString", "YourQueueName");
//await queueService.EnqueueMessageAsync("YourMessage");
