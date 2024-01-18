using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using System;
using Microsoft.Extensions.Configuration;


public class SharedQueriesService
{
    private readonly Container linksContainer;
    private readonly Container companiesContainer;
    private readonly Container chatbotsContainer;

    public SharedQueriesService(CosmosDbService cosmosDbService)
    {
        linksContainer = cosmosDbService.LinksContainer;
        companiesContainer = cosmosDbService.CompaniesContainer;
        chatbotsContainer = cosmosDbService.ChatbotsContainer;
    }

    public async Task<IEnumerable<Company>> GetAllCompanies()
    {
        string sqlQueryText = $"SELECT * FROM c";
        QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
        List<Company> companies = new List<Company>();
        using (FeedIterator<Company> feedIterator = companiesContainer.GetItemQueryIterator<Company>(queryDefinition))
        {
            while (feedIterator.HasMoreResults)
            {
                FeedResponse<Company> response = await feedIterator.ReadNextAsync();
                companies.AddRange(response.ToList());
            }
        }
        return companies;
    }

    public async Task<IEnumerable<Link>> GetAllLinks()
    {
        string sqlQueryText = $"SELECT * FROM c";
        QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
        List<Link> links = new List<Link>();
        using (FeedIterator<Link> feedIterator = linksContainer.GetItemQueryIterator<Link>(queryDefinition))
        {
            while (feedIterator.HasMoreResults)
            {
                FeedResponse<Link> response = await feedIterator.ReadNextAsync();
                links.AddRange(response.ToList());
            }
        }
        return links;
    }

    public async Task<IEnumerable<Chatbot>> GetAllChatbots()
    {
        string sqlQueryText = $"SELECT * FROM c";
        QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
        List<Chatbot> chatbots = new List<Chatbot>();
        using (FeedIterator<Chatbot> feedIterator = linksContainer.GetItemQueryIterator<Chatbot>(queryDefinition))
        {
            while (feedIterator.HasMoreResults)
            {
                FeedResponse<Chatbot> response = await feedIterator.ReadNextAsync();
                chatbots.AddRange(response.ToList());
            }
        }
        return chatbots;
    }
}

