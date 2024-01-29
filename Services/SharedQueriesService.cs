using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using System.Diagnostics;
using System;


public class SharedQueriesService
{
    private readonly Container conversationsContainer;
    private readonly Container companiesContainer;
    private readonly Container chatbotsContainer;
    private readonly Container linksContainer;
    private readonly Container messagesContainer;
    private readonly Container usersContainer;
    private readonly Container refinementsContainer;

    public SharedQueriesService(CosmosDbService cosmosDbService)
    {
        conversationsContainer = cosmosDbService.ConversationsContainer;
        companiesContainer = cosmosDbService.CompaniesContainer;
        chatbotsContainer = cosmosDbService.ChatbotsContainer;
        linksContainer = cosmosDbService.LinksContainer;
        messagesContainer = cosmosDbService.MessagesContainer;
        usersContainer = cosmosDbService.UsersContainer;
        refinementsContainer = cosmosDbService.RefinementsContainer;
    }

    public async Task<IEnumerable<T>> GetAllItems<T>(Container container)
    {
        string sqlQueryText = "SELECT * FROM c";
        QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
        List<T> items = new List<T>();

        using (FeedIterator<T> feedIterator = container.GetItemQueryIterator<T>(queryDefinition))
        {
            while (feedIterator.HasMoreResults)
            {
                FeedResponse<T> response = await feedIterator.ReadNextAsync();
                items.AddRange(response.ToList());
            }
        }
        return items;
    }

    public async Task<Message> GetMessageById(string msg_id, string convo_id)
    {
        var partitionKeyValue = new PartitionKey(convo_id);
        var response = await messagesContainer.ReadItemAsync<Message>(msg_id, partitionKeyValue);
        return response.Resource;
    }

    public async Task<IEnumerable<Message>> GetRecentMsgsForConvo(string convo_id, int limit)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string sqlQueryText = $"SELECT TOP {limit} * FROM m WHERE m.conversation_id = '{convo_id}' ORDER BY m._ts DESC";

        QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

        List<Message> messages = new List<Message>();
        using (FeedIterator<Message> feedIterator = messagesContainer.GetItemQueryIterator<Message>(queryDefinition))
        {
            while (feedIterator.HasMoreResults)
            {
                FeedResponse<Message> response = await feedIterator.ReadNextAsync();
                messages.AddRange(response.ToList());
            }
        }
        stopwatch.Stop();
        Console.WriteLine($"--> METRICS (COSMOS) Load cosmos data GetRecentMsgsForConvo: {stopwatch.ElapsedMilliseconds} ms");
        return messages;
    }

    public async Task<Conversation> GetConversationById(string convo_id)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        var partitionKeyValue = new PartitionKey(convo_id);
        var response = await conversationsContainer.ReadItemAsync<Conversation>(convo_id, partitionKeyValue);
        stopwatch.Stop();
        Console.WriteLine($"--> METRICS (COSMOS) Load cosmos data GetConversationById: {stopwatch.ElapsedMilliseconds} ms");
        return response.Resource;
    }

    public async Task<Company> GetCompanyById(string company_id)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string sqlQueryText;
        if (company_id == "all") sqlQueryText = $"SELECT * FROM c";
        else sqlQueryText = $"SELECT * FROM c WHERE c.company_id = '{company_id}'";

        QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

        Company company = null;
        using (FeedIterator<Company> feedIterator = companiesContainer.GetItemQueryIterator<Company>(queryDefinition))
        {
            while (feedIterator.HasMoreResults)
            {
                FeedResponse<Company> response = await feedIterator.ReadNextAsync();
                company = response.FirstOrDefault();
            }
        }
        stopwatch.Stop();
        Console.WriteLine($"--> METRICS (COSMOS) Load cosmos data GetCompanyById: {stopwatch.ElapsedMilliseconds} ms");
        return company;
    }

    public async Task<IEnumerable<Chatbot>> GetChatbotsByCompanyId(string company_id)
    {
        string sqlQueryText;
        if (company_id == "all") sqlQueryText = $"SELECT * FROM c";
        else sqlQueryText = $"SELECT * FROM c WHERE c.company_id = '{company_id}'";

        QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

        List<Chatbot> chatbots = new List<Chatbot>();
        using (FeedIterator<Chatbot> feedIterator = chatbotsContainer.GetItemQueryIterator<Chatbot>(queryDefinition))
        {
            while (feedIterator.HasMoreResults)
            {
                FeedResponse<Chatbot> response = await feedIterator.ReadNextAsync();
                chatbots.AddRange(response.ToList());
            }
        }
        return chatbots;
    }

    public async Task<Chatbot> GetFirstChatbotByCompanyId(string company_id)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string sqlQueryText;
        if (company_id == "all") sqlQueryText = $"SELECT * FROM c";
        else sqlQueryText = $"SELECT * FROM c WHERE c.company_id = '{company_id}'";

        QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

        Chatbot chatbot = null;
        using (FeedIterator<Chatbot> feedIterator = chatbotsContainer.GetItemQueryIterator<Chatbot>(queryDefinition))
        {
            while (feedIterator.HasMoreResults)
            {
                FeedResponse<Chatbot> response = await feedIterator.ReadNextAsync();
                chatbot = response.FirstOrDefault();
            }
        }
        stopwatch.Stop();
        Console.WriteLine($"--> METRICS (COSMOS) Load cosmos data GetFirstChatbotByCompanyId: {stopwatch.ElapsedMilliseconds} ms");
        return chatbot;
    }

    public async Task<IEnumerable<Refinement>> GetRefinementsByCompanyId(string company_id)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string sqlQueryText;
        if (company_id == "all") sqlQueryText = $"SELECT * FROM c";
        else sqlQueryText = $"SELECT * FROM c WHERE c.company_id = '{company_id}'";

        QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

        List<Refinement> refinements = new List<Refinement>();
        using (FeedIterator<Refinement> feedIterator = refinementsContainer.GetItemQueryIterator<Refinement>(queryDefinition))
        {
            while (feedIterator.HasMoreResults)
            {
                FeedResponse<Refinement> response = await feedIterator.ReadNextAsync();
                refinements.AddRange(response.ToList());
            }
        }
        stopwatch.Stop();
        Console.WriteLine($"--> METRICS (COSMOS) Load cosmos data GetRefinementsByCompanyId: {stopwatch.ElapsedMilliseconds} ms");
        return refinements;
    }

}

