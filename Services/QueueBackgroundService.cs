using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using SalesBotApi.Models;

public class QueueBackgroundService : BackgroundService
{
    private readonly QueueService queueService;
    private readonly WebpageProcessor webpageProcessor;
    private readonly MemoryStoreService memoryStoreService;
    // private readonly TelemetryClient telemetryClient;
    private readonly Container linksContainer;


    public QueueBackgroundService(
        QueueService _queueService, 
        WebpageProcessor _webpageProcessor,
        MemoryStoreService _memoryStoreService,
        CosmosDbService cosmosDbService
        // TelemetryClient _telemetryClient
    )
    {
        queueService = _queueService;
        webpageProcessor = _webpageProcessor;
        memoryStoreService = _memoryStoreService;
        // telemetryClient = _telemetryClient;
        linksContainer = cosmosDbService.LinksContainer;
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
            await Task.Delay(10000);
        }
    }

    private async void UpdateLink(Link link, string status, string result){
        List<PatchOperation> patchOperations = new List<PatchOperation>()
        {
            PatchOperation.Replace("/status", status),
            PatchOperation.Replace("/result", result),
            PatchOperation.Add("/scraped_timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };
        await linksContainer.PatchItemAsync<dynamic>(link.id, new PartitionKey(link.company_id), patchOperations);
    }

    private async void ProcessMessage(QueueMessage message)
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine("ProcessMessage");
        var base64EncodedBytes = Convert.FromBase64String(message.MessageText);
        var decodedMessage = Encoding.UTF8.GetString(base64EncodedBytes);
        Console.WriteLine(decodedMessage);
        var link = JsonConvert.DeserializeObject<Link>(decodedMessage);
        try{
            string[] chunks = await webpageProcessor.GetTextChunksFromUrlAsync(link.link, 1000);
            foreach (string chunk in chunks)
            {
                await memoryStoreService.Write(chunk, link.link, link.company_id);
            }
            UpdateLink(link, "complete", "success");
        } catch(Exception ex) {
            Console.WriteLine(ex.Message);
            UpdateLink(link, "error", ex.Message);
        }
        stopwatch.Stop();
    }
}
