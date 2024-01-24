using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos.Linq;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class RefinementsController : Controller
    {
        public class AddRefinementRequest {
            public string message_id { get; set; }
            public string convo_id { get; set; }
            public string question { get; set; }
            public string answer { get; set; }
            public bool is_positive { get; set; }
        }
        
        private readonly Container refinementsContainer;
        private readonly SharedQueriesService sharedQueriesService;

        public RefinementsController(CosmosDbService cosmosDbService, SharedQueriesService _sharedQueriesService)
        {
            refinementsContainer = cosmosDbService.RefinementsContainer;
            sharedQueriesService = _sharedQueriesService;
        }

        // GET: api/refinements
        [HttpGet]
        [JwtAuthorize]
        public async Task<ActionResult<IEnumerable<Refinement>>> GetRefinements()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;
            Response.Headers.Add("Access-Control-Allow-Origin", "*");

            IEnumerable<Chatbot> chatbots = await sharedQueriesService.GetChatbotsByCompanyId(company_id);
            var chatbotIds = string.Join("','", chatbots.Select(chatbot => chatbot.id));
            string sqlQueryText = $"SELECT * FROM c WHERE c.chatbot_id IN ('{chatbotIds}')";

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
            return refinements;
        }

        // POST: api/refinements
        [HttpPost]
        [JwtAuthorize]
        public async Task<IActionResult> AddRefinement([FromBody] AddRefinementRequest req)
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;

            if(req.message_id == null || req.convo_id == null) {
                return BadRequest();
            }
            
            Message msg;
            try {
                msg = await sharedQueriesService.GetMessageById(req.message_id, req.convo_id);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("Message not found");
            }

            if(company_id != "all" && msg.company_id != company_id){
                return Unauthorized();
            }

            Chatbot chatbot;
            try {
                IEnumerable<Chatbot> chatbots = await sharedQueriesService.GetChatbotsByCompanyId(msg.company_id);
                if(chatbots.Count() > 0) {
                    chatbot = chatbots.First();
                } else {
                    return NotFound("Chatbot not found (2)");
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("Chatbot not found (1)");
            }

            Refinement refinement = new Refinement
            {
                id = Guid.NewGuid().ToString(),
                company_id = msg.company_id,
                chatbot_id = chatbot.id,
                conversation_id = msg.conversation_id,
                message_id = msg.id,
                question = req.question,
                answer = req.answer,
                is_positive = req.is_positive
            };
            
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            await refinementsContainer.CreateItemAsync(refinement, new PartitionKey(chatbot.id));
            return NoContent();
        }
    }
}
