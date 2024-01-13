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
    public class MessagesController : Controller
    {
        private readonly Container messagesContainer;

        public MessagesController(CosmosDbService cosmosDbService)
        {
            messagesContainer = cosmosDbService.MessagesContainer;
        }

        // GET: api/messages
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Message>>> GetMessages(
            [FromQuery] string convo_id,
            [FromQuery] bool? latest
        )
        {
            if (
                (convo_id == null && latest == null) ||
                (convo_id != null && latest != null)
            )
            {
                return BadRequest($"Invalid or missing parameters");
            }

            string sqlQueryText = "";
            if (convo_id != null) {
                sqlQueryText = $"SELECT * FROM m WHERE m.conversation_id = '{convo_id}' ORDER BY m.timestamp ASC";
            }
            if (latest != null) {
                sqlQueryText = "SELECT  * FROM c WHERE c._ts >= (GetCurrentTimestamp() / 1000) - (30 * 24 * 60 * 60)";
            }
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

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return messages;
        }

        // GET: api/messages/count_per_convo
        [HttpGet("count_per_convo")]
        public async Task<ActionResult<IEnumerable<MessagesManyPerConvo>>> GetMessageCounts()
        {
            string sqlQueryText = "SELECT count(m) as many_msgs, m.conversation_id FROM m GROUP BY m.conversation_id";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            List<MessagesManyPerConvo> messages = new List<MessagesManyPerConvo>();
            using (FeedIterator<MessagesManyPerConvo> feedIterator = messagesContainer.GetItemQueryIterator<MessagesManyPerConvo>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<MessagesManyPerConvo> response = await feedIterator.ReadNextAsync();
                    messages.AddRange(response.ToList());
                }
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return messages;
        }
    }
}
