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

        public CompaniesController(CosmosDbService cosmosDbService)
        {
            companiesContainer = cosmosDbService.CompaniesContainer;
            usersContainer = cosmosDbService.UsersContainer;
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
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            string newUuid = Guid.NewGuid().ToString();
            string companyId = Regex.Replace(newCompanyReq.name.ToLower(), @"[^a-z0-9]", "");
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

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return Ok(newCompany);
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

    }
}
