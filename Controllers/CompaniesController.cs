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
    public class CompaniesController : Controller
    {
        private readonly Container companiesContainer;

        public CompaniesController(CosmosDbService cosmosDbService)
        {
            companiesContainer = cosmosDbService.CompaniesContainer;
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

//        // POST: api/companies
//        [HttpPost]
//        public async Task<IActionResult> CreateNewCompany([FromBody] NewCompanyRequest newCompanyReq)
//        {
//            try
//            {
//                await companiesContainer.ReplaceItemAsync(chatbot, chatbot.id);
//                return NoContent();
//            }
//            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
//            {
//                return NotFound();
//            }
//        }

    }
}
