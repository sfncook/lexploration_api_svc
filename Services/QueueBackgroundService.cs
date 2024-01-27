using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Hosting;

public class QueueBackgroundService : BackgroundService
{
    private readonly QueueService queueService;

    public QueueBackgroundService(QueueService _queueService)
    {
        queueService = _queueService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine("Poll");
            QueueMessage message = await queueService.GetScrapLinksMessageAsync();
            if (message != null)
            {
                try
                {
                    ProcessMessage(message);
                    await queueService.DeleteScrapLinksMessageAsync(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception");
                    Console.WriteLine(ex.ToString());
                }
            }
            await Task.Delay(1000);
        }
    }

    private void ProcessMessage(QueueMessage message)
    {
        Console.WriteLine("ProcessMessage");
        Console.WriteLine(message.MessageText);
        var base64EncodedBytes = Convert.FromBase64String(message.MessageText);
        var decodedMessage = Encoding.UTF8.GetString(base64EncodedBytes);
        Console.WriteLine(decodedMessage);
    }
}
