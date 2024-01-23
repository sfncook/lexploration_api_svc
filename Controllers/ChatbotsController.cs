using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class ChatbotsController : Controller
    {
        private readonly Container chatbotsContainer;
        private readonly SharedQueriesService queriesSvc;

        public ChatbotsController(CosmosDbService cosmosDbService, SharedQueriesService _queriesSvc)
        {
            chatbotsContainer = cosmosDbService.ChatbotsContainer;
            queriesSvc = _queriesSvc;
        }

        // GET: api/chatbots
        [HttpGet]
        [JwtAuthorize]
        public async Task<ActionResult<IEnumerable<Chatbot>>> GetChatbots()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            IEnumerable<Chatbot> chatbots = await GetChatbots(company_id);
            return Ok(chatbots);
        }

        // GET: api/chatbots/client?company_id=...
        [HttpGet("client")]
        public async Task<ActionResult<Chatbot>> GetChatbotsNoAuth([FromQuery] string company_id)
        {
            if (company_id == null)
            {
                return BadRequest("Missing company_id parameter");
            }
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            IEnumerable<Chatbot> chatbots = await GetChatbots(company_id);
            if(chatbots.Count() == 0) {
                return NotFound();
            }
            return Ok(chatbots.First());
        }

        // PUT: api/chatbots
        [HttpPut]
        [JwtAuthorize]
        public async Task<IActionResult> UpdateChatbot([FromBody] Chatbot chatbot)
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;
            if(company_id != "all") {
                if(chatbot.company_id != company_id) {
                    return Unauthorized();
                }
            }

            try
            {
                chatbot.initialized = true;
                await chatbotsContainer.ReplaceItemAsync(chatbot, chatbot.id);
                return NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }

        private async Task<IEnumerable<Chatbot>> GetChatbots(string company_id) {
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
    }
}
