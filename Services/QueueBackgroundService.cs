using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using SalesBotApi.Models;

public class QueueBackgroundService : BackgroundService
{
    private readonly QueueService queueService;
    private readonly WebpageProcessor webpageProcessor;
    private readonly MemoryStoreService memoryStoreService;
    private readonly TelemetryClient telemetryClient;


    public QueueBackgroundService(
        QueueService _queueService, 
        WebpageProcessor _webpageProcessor,
        MemoryStoreService _memoryStoreService,
        TelemetryClient _telemetryClient
    )
    {
        queueService = _queueService;
        webpageProcessor = _webpageProcessor;
        memoryStoreService = _memoryStoreService;
        telemetryClient = _telemetryClient;
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

    private async void ProcessMessage(QueueMessage message)
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine("ProcessMessage");
        var base64EncodedBytes = Convert.FromBase64String(message.MessageText);
        var decodedMessage = Encoding.UTF8.GetString(base64EncodedBytes);
        Console.WriteLine(decodedMessage);
        var link = JsonConvert.DeserializeObject<Link>(decodedMessage);
        // Console.WriteLine(link.id);
        // Console.WriteLine(link.link);
        // Console.WriteLine(link.company_id);
        string[] chunks = await webpageProcessor.GetTextChunksFromUrlAsync(link.link, 1000);
        foreach (string chunk in chunks)
        {
            await memoryStoreService.Write(chunk, link.link, link.company_id);
        }
        stopwatch.Stop();
        telemetryClient.TrackMetric("links_scrape_ms", stopwatch.Elapsed.TotalMilliseconds);
        telemetryClient.TrackMetric(new MetricTelemetry("LinkScrape", stopwatch.Elapsed.TotalMilliseconds) { 
            Properties = { { "_MS.MetricNamespace", "SalesBotMetrics" } } 
        });


    }
}
