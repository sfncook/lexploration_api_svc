using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SalesBotApi.Models;

public class BackgroundService_Queue_LinkScrape : BackgroundService
{
    private readonly QueueService<Link> queueService;
    private readonly WebpageProcessor webpageProcessor;
    private readonly MemoryStoreService memoryStoreService;
    // private readonly TelemetryClient telemetryClient;
    private readonly Container linksContainer;
    private readonly LogBufferService logger;
    private readonly MySettings mySettings;


    public BackgroundService_Queue_LinkScrape(
        QueueService<Link> _queueService, 
        WebpageProcessor _webpageProcessor,
        MemoryStoreService _memoryStoreService,
        CosmosDbService cosmosDbService,
        LogBufferService logger,
        IOptions<MySettings> mySettings
        // TelemetryClient _telemetryClient
    )
    {
        queueService = _queueService;
        webpageProcessor = _webpageProcessor;
        memoryStoreService = _memoryStoreService;
        // telemetryClient = _telemetryClient;
        linksContainer = cosmosDbService.LinksContainer;
        this.logger = logger;
        this.mySettings = mySettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await queueService.GetMessageAsync();
            if (result != null)
            {
                QueueMessage queueMessage = result.Value.Item1;
                Link link = result.Value.Item2;
                try
                {
                    ProcessMessage(link);
                    await queueService.DeleteMessageAsync(queueMessage);
                }
                catch (Exception ex)
                {
                    logger.Error("Exception");
                    logger.Error(ex.ToString());
                }
            } else {
                // When there is no message waiting then sleep between polls
                await Task.Delay(mySettings.LinkScrapeQueuePollRateMs, stoppingToken);
            }
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

    private async void ProcessMessage(Link link)
    {
        var stopwatch = Stopwatch.StartNew();
        try{
            string[] chunks = await webpageProcessor.GetTextChunksFromUrlAsync(link.link, 1000);
            foreach (string chunk in chunks)
            {
                await memoryStoreService.Write(chunk, link.link, link.company_id);
            }
            UpdateLink(link, "complete", "success");
        } catch(Exception ex) {
            logger.Error(ex.Message);
            UpdateLink(link, "error", ex.Message);
        }
        stopwatch.Stop();
    }
}
