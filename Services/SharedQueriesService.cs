using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;


public class SharedQueriesService
{
    private readonly Container conversationsContainer;
    private readonly Container companiesContainer;
    private readonly Container chatbotsContainer;
    private readonly Container linksContainer;
    private readonly Container messagesContainer;
    private readonly Container usersContainer;

    public SharedQueriesService(CosmosDbService cosmosDbService)
    {
        conversationsContainer = cosmosDbService.ConversationsContainer;
        companiesContainer = cosmosDbService.CompaniesContainer;
        chatbotsContainer = cosmosDbService.ChatbotsContainer;
        linksContainer = cosmosDbService.LinksContainer;
        messagesContainer = cosmosDbService.MessagesContainer;
        usersContainer = cosmosDbService.UsersContainer;
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

    public async Task<Conversation> GetConversationById(string convo_id)
    {
        var partitionKeyValue = new PartitionKey(convo_id);
        var response = await conversationsContainer.ReadItemAsync<Conversation>(convo_id, partitionKeyValue);
        return response.Resource;
    }

    public async Task<Company> GetCompanyById(string company_id)
    {
        var partitionKeyValue = new PartitionKey(company_id);
        var response = await conversationsContainer.ReadItemAsync<Company>(company_id, partitionKeyValue);
        return response.Resource;
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
        return chatbot;
    }

}

