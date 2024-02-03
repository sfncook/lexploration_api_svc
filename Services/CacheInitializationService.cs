using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using SalesBotApi.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// This class runs in the background on startup and pre-populates the in-memory cache with 
//  *all* companies and chatbots in the cosmos database.  This is a temporary measure
//  to get our response times super low, especially for the chatbot web UI.
public class CacheInitializationService : BackgroundService
{
    private readonly Container companiesContainer;
    private readonly Container chatbotsContainer;
    private readonly SharedQueriesService queriesSvc;
    private readonly ICacheProvider<Company> cacheCompany;
    private readonly ICacheProvider<Chatbot> cacheChatbot;
    private readonly LogBufferService logger;

    public CacheInitializationService(
        CosmosDbService cosmosDbService,
        SharedQueriesService queriesSvc, 
        InMemoryCacheService<Company> cacheCompany,
        InMemoryCacheService<Chatbot> cacheChatbot,
        LogBufferService logger
    )
    {
        companiesContainer = cosmosDbService.CompaniesContainer;
        chatbotsContainer = cosmosDbService.ChatbotsContainer;
        this.queriesSvc = queriesSvc;
        this.cacheCompany = cacheCompany;
        this.cacheChatbot = cacheChatbot;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var companiesTask = queriesSvc.GetAllItems<Company>(companiesContainer);
                var chatbotsTask = queriesSvc.GetAllItems<Chatbot>(chatbotsContainer);
                await Task.WhenAll(
                    companiesTask,
                    chatbotsTask
                );

                IEnumerable<Company> companies = await companiesTask;
                foreach(Company c in companies) {
                    if(c.company_id!="XXX" && c.company_id!="all"){
                        cacheCompany.Set(c.company_id, c);
                    }
                }

                IEnumerable<Chatbot> chatbots = await chatbotsTask;
                foreach(Chatbot c in chatbots) {
                    cacheChatbot.Set(c.company_id, c);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
        }
    }
}
