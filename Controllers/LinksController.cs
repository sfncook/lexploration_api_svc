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
using System.Text;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class LinksController : Controller
    {
        private readonly Container linksContainer;
        private readonly IHttpClientFactory _clientFactory;
        private readonly Container companiesContainer;

        public LinksController(CosmosDbService cosmosDbService, IHttpClientFactory clientFactory)
        {
            linksContainer = cosmosDbService.LinksContainer;
            _clientFactory = clientFactory;
            companiesContainer = cosmosDbService.CompaniesContainer;
        }

        // GET: api/links
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Link>>> GetLinks(
            [FromQuery] string company_id
        )
        {
            if (company_id == null)
            {
                return BadRequest("Missing company_id parameter");
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return Ok(await GetAllLinksForCompany(company_id));
        }

        // GET: api/links/xxx-yyy-zzz
        [HttpGet("{link_id}")]
        public async Task<ActionResult<Link>> GetLinkWithId(string link_id)
        {
            if (link_id == null)
            {
                return BadRequest("Missing link id in path");
            }

            string sqlQueryText = $"SELECT * FROM c WHERE c.id = '{link_id}' OFFSET 0 LIMIT 1";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            Link link = null;
            using (FeedIterator<Link> feedIterator = linksContainer.GetItemQueryIterator<Link>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<Link> response = await feedIterator.ReadNextAsync();
                    link = response.First();
                    break;
                }
            }
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return link;
        }

        // PUT: api/links
        [HttpPut]
        public async Task<IActionResult> PutTodoItem([FromBody] Link link)
        {
            try
            {
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                await linksContainer.ReplaceItemAsync(link, link.id);
                return NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }

        // POST: api/links
        [HttpPost]
        public async Task<IActionResult> AddLink([FromQuery] string company_id, [FromBody] AddLinkRequest addLinkRequest)
        {
            if (company_id == null)
            {
                return BadRequest("Missing company_id query parameter");
            }
            if (addLinkRequest == null)
            {
                return BadRequest("Missing link text in body");
            }

            string _id = Guid.NewGuid().ToString();
            Link link = new Link()
            {
                 id = _id,
                 link = addLinkRequest.link,
                 company_id = company_id,
                 status = "",
                 result = "",
            };

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            await linksContainer.CreateItemAsync<Link>(link, new PartitionKey(link.company_id));
            return link;
//            string _id = Guid.NewGuid().ToString();
//            Link link = new Link()
//            {
//                 id = _id,
//                 link = addLinkRequest.link,
//                 company_id = company_id,
//                 status = "",
//                 result = "",
//            };
//            await linksContainer.CreateItemAsync<Link>(link, new PartitionKey(link.company_id));

            // Make POST request to the downstream service
            HttpClient client = _clientFactory.CreateClient();
            string postUrl = $"http://localhost:7071/api/acquire_links?companyid={company_id}&url={Uri.EscapeDataString(addLinkRequest.link)}";
            var postResponse = await client.PostAsync(postUrl, new StringContent("", Encoding.UTF8, "application/json"));

            if (!postResponse.IsSuccessStatusCode)
            {
                return StatusCode((int)postResponse.StatusCode, $"Error calling downstream service: {postResponse.ReasonPhrase}");
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return NoContent();
        }

        // POST: api/links/scrape
        [HttpPost("scrape")]
        public async Task<IActionResult> AddLink([FromQuery] string company_id)
        {
            if (company_id == null)
            {
                return BadRequest("Missing company_id query parameter");
            }

            //TODO
            await SetCompanyTraining(company_id);

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return NoContent();
        }

        private async Task<ItemResponse<dynamic>> SetCompanyTraining(string company_id) {
            Company company = await GetCompanyById(company_id);
            List<PatchOperation> patchOperations = new List<PatchOperation>()
            {
                PatchOperation.Add("/training", true)
            };
            return await companiesContainer.PatchItemAsync<dynamic>(company.id, new PartitionKey(company_id), patchOperations);
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

        private async Task<IEnumerable<Link>> GetAllLinksForCompany(string company_id)
        {
            string sqlQueryText;
            if (company_id == "all") sqlQueryText = $"SELECT * FROM c";
            else sqlQueryText = $"SELECT * FROM c WHERE c.company_id = '{company_id}'";

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            List<Link> links = new List<Link>();
            using (FeedIterator<Link> feedIterator = linksContainer.GetItemQueryIterator<Link>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<Link> response = await feedIterator.ReadNextAsync();
                    links.AddRange(response.ToList());
                }
            }
            return links;
        }
    }
}
