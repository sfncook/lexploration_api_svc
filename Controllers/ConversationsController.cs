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

        public ConversationsController(CosmosDbService cosmosDbService)
        {
            conversationsContainer = cosmosDbService.ConversationsContainer;
            messagesContainer = cosmosDbService.MessagesContainer;
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


        // Stupid fucking hack because Azure functions are pure dog shit.
        // GET: api/conversations/verify
        [HttpGet("verify")]
        public async Task<IActionResult> VerifyConversationById(
            [FromQuery] string convo_id
        )
        {
            if (convo_id == null) {
                return BadRequest();
            }

            string sqlQueryText = $"SELECT * FROM c WHERE c.id = '{convo_id}'  OFFSET 0 LIMIT 1";
            Console.WriteLine(sqlQueryText);

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

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            if(conversations.Count() > 0) return NoContent();
            else return NotFound();
        }
    }
}
