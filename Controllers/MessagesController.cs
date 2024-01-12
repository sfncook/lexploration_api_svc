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
        public async Task<ActionResult<IEnumerable<Message>>> GetConversations([FromQuery] string convo_id)
        {
            if (convo_id == null)
            {
                return BadRequest($"Invalid or missing parameter convo_id");
            }
            string sqlQueryText = $"SELECT * FROM m WHERE m.conversation_id = '{convo_id}' ORDER BY m.timestamp ASC";
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

            return messages;
        }
    }
}
