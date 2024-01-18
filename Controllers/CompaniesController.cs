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
using System.Text.RegularExpressions;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class CompaniesController : Controller
    {
        private readonly Container companiesContainer;
        private readonly Container usersContainer;
        private readonly Container chatbotsContainer;

        public CompaniesController(CosmosDbService cosmosDbService)
        {
            companiesContainer = cosmosDbService.CompaniesContainer;
            usersContainer = cosmosDbService.UsersContainer;
            chatbotsContainer = cosmosDbService.ChatbotsContainer;
        }

        // GET: api/companies
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Company>>> GetCompanies(
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

            List<Company> companies = new List<Company>();
            using (FeedIterator<Company> feedIterator = companiesContainer.GetItemQueryIterator<Company>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<Company> response = await feedIterator.ReadNextAsync();
                    companies.AddRange(response.ToList());
                }
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return companies;
        }

        // POST: api/companies
        [HttpPost]
        public async Task<ActionResult<Company>> CreateNewCompany([FromBody] NewCompanyRequest newCompanyReq)
        {
            if (newCompanyReq.user_id == null || newCompanyReq.name == null || newCompanyReq.description == null)
            {
                return BadRequest("Invalid request, missing parameters");
            }

            FullUser user = await GetUserById(newCompanyReq.user_id);
            string oldPartitionKeyValue = user.company_id;
            if(oldPartitionKeyValue == null || oldPartitionKeyValue == "") {
                Console.WriteLine($"oldPartitionKeyValue is empty or null:{oldPartitionKeyValue} - This means this request will probably fail.  You need to prime new accounts with 'company_id' == 'XXX'");
                return BadRequest("Invalid request, check the logs");
            }
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            string newUuid = Guid.NewGuid().ToString();
            string companyId = Regex.Replace(newCompanyReq.name.ToLower(), @"[^a-z0-9]", "");

            Company company = await GetCompanyById(companyId);
            if(company != null)
            {
                return Conflict("Company already exists");
            }

            user.company_id = companyId;
            await usersContainer.CreateItemAsync(user, new PartitionKey(user.company_id));
            await usersContainer.DeleteItemAsync<FullUser>(user.id, new PartitionKey(oldPartitionKeyValue));
            await usersContainer.ReplaceItemAsync(user, user.id, new PartitionKey(user.company_id));

            Company newCompany = new Company
            {
                id = newUuid,
                company_id = companyId,
                name = newCompanyReq.name,
                description = newCompanyReq.description,
            };

            await companiesContainer.CreateItemAsync(newCompany, new PartitionKey(companyId));

            Chatbot newChatbot = new Chatbot
            {
                id = Guid.NewGuid().ToString(),
                company_id = companyId,
                show_avatar = true,
                llm_model = "keli-35-turbo",
                contact_prompt = "Try to get the user to tell you their name, email, and phone number",
                contact_link = "",
                contact_method = "contact_form",
                greeting = $"Hi! I'm Keli! I can answer your questions about {newCompany.name}.",
                initialized = false,
            };
            await chatbotsContainer.CreateItemAsync(newChatbot, new PartitionKey(companyId));

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return Ok(newCompany);
        }

        // PUT: api/companies
        [HttpPut]
        public async Task<IActionResult> UpdateCompany([FromBody] Company company)
        {
            try
            {
                List<PatchOperation> patchOperations = new List<PatchOperation>()
                {
                    PatchOperation.Replace("/name", company.name),
                    PatchOperation.Replace("/description", company.description)
                };
                await companiesContainer.PatchItemAsync<dynamic>(company.id, new PartitionKey(company.company_id), patchOperations);
//                await companiesContainer.ReplaceItemAsync(company, company.id);

                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                return NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }

        private async Task<FullUser> GetUserById(string user_id) {
            string sqlQueryText = $"SELECT * FROM c WHERE c.id = '{user_id}' OFFSET 0 LIMIT 1";

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            FullUser user = null;
            using (FeedIterator<FullUser> feedIterator = usersContainer.GetItemQueryIterator<FullUser>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<FullUser> response = await feedIterator.ReadNextAsync();
                    user = response.First();
                    break;
                }
            }
            return user;
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
