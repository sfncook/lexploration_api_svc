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
        private readonly Container companiesContainer;
        private readonly Container messagesContainer;

        public ConversationsController(CosmosDbService cosmosDbService)
        {
            conversationsContainer = cosmosDbService.ConversationsContainer;
            companiesContainer = cosmosDbService.CompaniesContainer;
            messagesContainer = cosmosDbService.MessagesContainer;
        }

        // GET: api/conversations
        [HttpGet]
        [JwtAuthorize]
        public async Task<ActionResult<IEnumerable<Conversation>>> GetConversations(
            [FromQuery] int? since_timestamp,
            [FromQuery] bool? latest,
            [FromQuery] bool? with_user_data
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
            if (latest != null) {
                sqlQueryText = "SELECT  * FROM c WHERE c._ts >= (GetCurrentTimestamp() / 1000) - (30 * 24 * 60 * 60)";
            }
            if (with_user_data != null) {
                sqlQueryText = "SELECT * FROM c WHERE IS_STRING(c.user_email) OR IS_STRING(c.user_phone_number) OR IS_STRING(c.user_first_name) OR IS_STRING(c.user_last_name)";
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
