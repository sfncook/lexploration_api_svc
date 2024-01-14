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
    public class ChatbotsController : Controller
    {
        private readonly Container chatbotsContainer;

        public ChatbotsController(CosmosDbService cosmosDbService)
        {
            chatbotsContainer = cosmosDbService.ChatbotsContainer;
        }

        // GET: api/chatbots
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Chatbot>>> GetChatbots(
            [FromQuery] string company_id
        )
        {
            if (company_id == null)
            {
                return BadRequest("Missing company_id parameter");
            }

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

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return chatbots;
        }

    }
}
