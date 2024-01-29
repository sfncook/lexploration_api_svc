using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using System;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class ConversationsController : Controller
    {
        private readonly Container conversationsContainer;
        private readonly Container messagesContainer;
        private readonly InMemoryCacheService<Conversation> cacheConvo;
        private readonly SharedQueriesService sharedQueriesService;

        public ConversationsController(
            CosmosDbService cosmosDbService,
            InMemoryCacheService<Conversation> cacheConvo,
            SharedQueriesService sharedQueriesService
        )
        {
            conversationsContainer = cosmosDbService.ConversationsContainer;
            messagesContainer = cosmosDbService.MessagesContainer;
            this.cacheConvo = cacheConvo;
            this.sharedQueriesService = sharedQueriesService;
        }

        // GET: api/conversations
        [HttpGet]
        [JwtAuthorize]
        public async Task<ActionResult<IEnumerable<Conversation>>> GetConversations(
            [FromQuery] int? since_timestamp
        )
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;
            string sqlQueryText = "";
            if (company_id != null) {
                if(company_id != "all") {
                    sqlQueryText = $"SELECT * FROM c WHERE c.company_id = '{company_id}'";
                } else {
                    sqlQueryText = $"SELECT * FROM c";
                }
            }
            if (since_timestamp != null) {
                bool containsWhere = sqlQueryText.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase) >= 0;
                if (containsWhere) sqlQueryText += " AND ";
                else sqlQueryText += " WHERE ";
                //(GetCurrentTimestamp() / 1000) - (30 * 24 * 60 * 60)
                sqlQueryText += $"c._ts >= {since_timestamp}";
            }
            sqlQueryText += " ORDER BY c._ts DESC";

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            List<Conversation> conversations = new List<Conversation>();
            using (FeedIterator<Conversation> feedIterator = conversationsContainer.GetItemQueryIterator<Conversation>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<Conversation> response = await feedIterator.ReadNextAsync();
                    conversations.AddRange(response.ToList());
                }
            }
            foreach(Conversation c in conversations) {
                cacheConvo.Set(c.id, c);
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return conversations;
        }

        // DELETE: api/conversations
        [HttpDelete]
        [JwtAuthorize]
        public async Task<IActionResult> DeleteConvo([FromQuery] string convo_id)
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string role = userData.role;
            if(role != "root") {
                return Unauthorized();
            }

            if(convo_id == null) {
                return BadRequest("Missing convo_id parameter");
            }

            await conversationsContainer.DeleteItemAsync<Conversation>(convo_id, new PartitionKey(convo_id));
            cacheConvo.Clear(convo_id);

            string sqlQueryText = $"SELECT * FROM m WHERE m.conversation_id = '{convo_id}'";
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

            List<Task> deleteTasks = new List<Task>();
            foreach (var message in messages)
            {
                Task deleteTask = messagesContainer.DeleteItemAsync<Message>(message.id, new PartitionKey(message.conversation_id));
                deleteTasks.Add(deleteTask);
            }
            await Task.WhenAll(deleteTasks);

            return NoContent();
        }


        // No-Auth for chat client
        // GET: api/conversations/verify
        [HttpGet("verify")]
        public async Task<IActionResult> VerifyConversationById(
            [FromQuery] string convo_id
        )
        {
            if (convo_id == null) {
                return BadRequest();
            }
            Conversation conversation = await sharedQueriesService.GetConversationById(convo_id);

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            if(conversation != null) return NoContent();
            else return NotFound();
        }

        // No-Auth for chat client
        // POST: api/conversations/create
        [HttpPost("create")]
        public async Task<ActionResult<Conversation>> CreateNewConvo(
            [FromQuery] string companyid
        )
        {
            if (companyid == null) {
                return BadRequest();
            }

            Company company = await sharedQueriesService.GetCompanyById(companyid);
            if(company==null) {
                return NotFound();
            }

            var id = Guid.NewGuid().ToString();
            Conversation newConvo = new Conversation
            {
                id = id,
                company_id = companyid,
                user_id = id
            };

            await conversationsContainer.CreateItemAsync(newConvo, new PartitionKey(id));
            cacheConvo.Clear(newConvo.id);

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return Ok(newConvo);
        }
    }
}
