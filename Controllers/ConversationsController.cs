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
    public class ConversationsController : Controller
    {
        private readonly Container conversationsContainer;
        private readonly Container companiesContainer;

        public ConversationsController(CosmosDbService cosmosDbService)
        {
            conversationsContainer = cosmosDbService.ConversationsContainer;
            companiesContainer = cosmosDbService.CompaniesContainer;
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
