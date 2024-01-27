using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
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
            "scrape-links-dev"
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

    public async Task<QueueMessage> GetScrapLinksMessageAsync()
    {
        QueueMessage[] retrievedMessage = await scrapeLinksQueueClient.ReceiveMessagesAsync(maxMessages: 1);
        if (retrievedMessage.Length > 0)
        {
            var message = retrievedMessage[0];
            return message;
        }

        return null;
    }

    public async Task DeleteScrapLinksMessageAsync(QueueMessage message)
    {
        await scrapeLinksQueueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
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
