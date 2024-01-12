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
        }

        // GET: api/Admin
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

        // GET: api/Todo/foo_001
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
    }
}
