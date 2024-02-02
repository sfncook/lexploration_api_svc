using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SalesBotApi.Models;
using System;
using System.Text;
using System.Threading.Tasks;

public class QueueService
{
    private readonly QueueClient scrapeLinksQueueClient;
    private readonly QueueClient sendEmailQueueClient;
    private readonly LogBufferService logger;

    public QueueService(
        IOptions<MySettings> _mySettings,
        IOptions<MyConnectionStrings> _myConnectionStrings,
        LogBufferService logger
    )
    {
        MySettings mySettings = _mySettings.Value;
        MyConnectionStrings myConnectionStrings = _myConnectionStrings.Value;
        this.logger = logger;

        scrapeLinksQueueClient = new QueueClient(
            myConnectionStrings.QueueConnectionStr,
            mySettings.QueueLinks
        );
        scrapeLinksQueueClient.CreateIfNotExists();

        sendEmailQueueClient = new QueueClient(
            myConnectionStrings.QueueConnectionStr,
            mySettings.QueueEmails
        );
        sendEmailQueueClient.CreateIfNotExists();
    }

    public async Task EnqueueScrapLinksMessageAsync(string message)
    {
        logger.Info(message);
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
        logger.Info(message);
        if (string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));

        var bytes = Encoding.UTF8.GetBytes(message);
        await sendEmailQueueClient.SendMessageAsync(Convert.ToBase64String(bytes));
    }
}

// Usage
//var queueService = new QueueService("YourConnectionString", "YourQueueName");
//await queueService.EnqueueMessageAsync("YourMessage");
