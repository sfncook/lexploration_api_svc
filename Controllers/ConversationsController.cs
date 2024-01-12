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

        public ConversationsController()
        {
            CosmosClient client = new CosmosClient(
                "https://keli-chatbot-02.documents.azure.com:443/",
                "r5Mvqb5nf0G9uILKYTl0XTQHSMcxerm65qwm22ePQTIhQTxqnSPk8qosd2qaNjT0zx25XhK1i6jvACDbEcDLTg==",
                new CosmosClientOptions()
                {
                    ApplicationRegion = Regions.EastUS2,
                });
            Database database = client.GetDatabase("keli");
            conversationsContainer = database.GetContainer("conversations_sales");
            companiesContainer = database.GetContainer("companies_sales");
        }

        // POST: api/conversations
        [HttpPost]
        public async Task<IActionResult> CreateConversation([FromQuery] string company_id)
        {
            Company company = await GetCompanyById(company_id);
            if (company == null)
            {
                return NotFound($"Company with ID {company_id} not found.");
            }

            string _id = Guid.NewGuid().ToString();
            Conversation conversation = new Conversation()
            {
                 id = _id,
                 user_id = _id,
                 company_id = company.company_id
            };

            await conversationsContainer.CreateItemAsync<Conversation>(conversation, new PartitionKey(_id));

            return new JsonResult(conversation)
            {
                StatusCode = 200
                // Add additional headers if needed
            };
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
