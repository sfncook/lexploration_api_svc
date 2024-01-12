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

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class AdminController : Controller
    {
        private readonly Container messagesContainer;
        private readonly Container conversationsContainer;
        private readonly Container companiesContainer;

        public AdminController()
        {
            CosmosClient client = new CosmosClient(
                "https://keli-chatbot-02.documents.azure.com:443/",
                "r5Mvqb5nf0G9uILKYTl0XTQHSMcxerm65qwm22ePQTIhQTxqnSPk8qosd2qaNjT0zx25XhK1i6jvACDbEcDLTg==",
                new CosmosClientOptions()
                {
                    ApplicationRegion = Regions.EastUS2,
                });
            Database database = client.GetDatabase("keli");
            messagesContainer = database.GetContainer("messages_sales");
            conversationsContainer = database.GetContainer("conversations_sales");
            companiesContainer = database.GetContainer("companies_sales");
        }

        // GET: api/admin
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Message>>> GetTodoItems([FromQuery] int offset = 0, [FromQuery] int limit = 10)
        {
            Console.WriteLine("YO MAMA XXX");

            // Define a SQL query string to get the first 2 messages
//            string sqlQueryText = $"SELECT * FROM c ORDER BY c._ts DESC OFFSET {offset} LIMIT {limit}";
            string sqlQueryText = $"SELECT * FROM c WHERE IS_STRING(c.company_id) OFFSET {offset} LIMIT {limit}";
//            string sqlQueryText = $"SELECT count(*) FROM c";
            Console.WriteLine(sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            List<Message> messages = new List<Message>();

            using (FeedIterator<Message> feedIterator = messagesContainer.GetItemQueryIterator<Message>(
                queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<Message> response = await feedIterator.ReadNextAsync();
                    messages.AddRange(response.ToList());
                }
            }

            return messages;
        }

        // GET: api/admin/foo_001
        [HttpGet("foo_001")]
        public async Task<ActionResult<IEnumerable<MessagesManyPerConvo>>> GetTodoItems()
        {
            Console.WriteLine("YO MAMA foo_001");

            // Define a SQL query string to get the first 2 messages
            string sqlQueryText = "SELECT count(m) as many_msgs, m.conversation_id FROM m GROUP BY m.conversation_id";
            Console.WriteLine(sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            List<MessagesManyPerConvo> messages = new List<MessagesManyPerConvo>();

            using (FeedIterator<MessagesManyPerConvo> feedIterator = messagesContainer.GetItemQueryIterator<MessagesManyPerConvo>(
                queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<MessagesManyPerConvo> response = await feedIterator.ReadNextAsync();
                    messages.AddRange(response.ToList());
                }
            }

            return messages;
        }

        // POST: api/admin/conversations
        [HttpPost("conversations")]
        public async Task<IActionResult> CreateConversation([FromQuery] string company_id)
        {
            Company company = await GetCompanyById(company_id);
            if (company == null)
            {
                return NotFound($"Company with ID {company_id} not found.");
            }

            string _id = Guid.NewGuid().ToString();
            Conversation conversation = new Conversation()
            {
                 id = _id,
                 user_id = _id,
                 company_id = company.company_id
            };

            await conversationsContainer.CreateItemAsync<Conversation>(conversation, new PartitionKey(_id));

            return new JsonResult(conversation)
            {
                StatusCode = 200
                // Add additional headers if needed
            };
        }

        private async Task<Company> GetCompanyById(string company_id)
        {
            string sqlQueryText = $"SELECT * FROM c WHERE c.company_id = '{company_id}'";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            FeedIterator<Company> feedIterator = companiesContainer.GetItemQueryIterator<Company>(queryDefinition);

            Company company = null;
            while (feedIterator.HasMoreResults)
            {
                FeedResponse<Company> response = await feedIterator.ReadNextAsync();
                if (response.Count > 0)
                {
                    company = response.First();
                    break;
                }
            }

            return company;
        }

    }
}
