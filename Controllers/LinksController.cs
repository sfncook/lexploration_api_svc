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
        private readonly QueueService queueService;
        private readonly SharedQueriesService queriesSvc;

        public LinksController(
            CosmosDbService cosmosDbService,
            IHttpClientFactory clientFactory,
            QueueService _queueService,
            SharedQueriesService _queriesSvc
        )
        {
            linksContainer = cosmosDbService.LinksContainer;
            _clientFactory = clientFactory;
            companiesContainer = cosmosDbService.CompaniesContainer;
            queueService = _queueService;
            queriesSvc = _queriesSvc;
        }

        // GET: api/links
        [HttpGet]
        [JwtAuthorize]
        public async Task<ActionResult<IEnumerable<Link>>> GetLinks()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return Ok(await GetAllLinksForCompany(company_id));
        }

        // GET: api/links/xxx-yyy-zzz
        [HttpGet("{link_id}")]
        [JwtAuthorize]
        public async Task<ActionResult<Link>> GetLinkWithId(string link_id)
        {
            if (link_id == null)
            {
                return BadRequest("Missing link id in path");
            }

            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;

            string sqlQueryText = $"SELECT * FROM c WHERE c.id = '{link_id}'";
            if(company_id != "all") {
                sqlQueryText += $" AND c.company_id = '{company_id}'";
            }
            sqlQueryText += " OFFSET 0 LIMIT 1";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            Link link = null;
            using (FeedIterator<Link> feedIterator = linksContainer.GetItemQueryIterator<Link>(queryDefinition))
            {
                if (feedIterator.HasMoreResults)
                {
                    FeedResponse<Link> response = await feedIterator.ReadNextAsync();
                    link = response.FirstOrDefault(); // This will not throw an exception if the response is empty
                }
            }
            if(link != null)
            {
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                return link;
            } else {
                return NotFound();
            }
        }

        // PUT: api/links
        [HttpPut]
        [JwtAuthorize]
        public async Task<IActionResult> UpdateLink([FromBody] Link link)
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;
            if(company_id != "all") {
                if(link.company_id != company_id) {
                    return Unauthorized();
                }
            }

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
        [JwtAuthorize]
        public async Task<IActionResult> AddLink([FromBody] AddLinkRequest addLinkRequest)
        {
            if (addLinkRequest == null)
            {
                return BadRequest("Missing link text in body");
            }

            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;

            // Make POST request to the downstream service
            HttpClient client = _clientFactory.CreateClient();
            string postUrl = $"https://salesbot-001.azurewebsites.net/api/acquire_links?companyid={company_id}&url={Uri.EscapeDataString(addLinkRequest.link)}";
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
        [JwtAuthorize]
        public async Task<IActionResult> StartScrapingLinks()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;

            await SetCompanyTraining(company_id);
            IEnumerable<Link> allLinks = await GetAllLinksForCompany(company_id);
            var filteredLinks = allLinks.Where(link => string.IsNullOrWhiteSpace(link.status));
            int batchSize = 10;
            var linkBatches = BatchLinks(filteredLinks, batchSize);
            foreach (var batch in linkBatches)
            {
                var tasks = batch.Select(link =>
                    queueService.EnqueueMessageAsync(JsonConvert.SerializeObject(link)))
                    .ToList();
                await Task.WhenAll(tasks);
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return NoContent();
        }

        private IEnumerable<IEnumerable<Link>> BatchLinks(IEnumerable<Link> links, int batchSize)
        {
            int total = 0;
            while (total < links.Count())
            {
                yield return links.Skip(total).Take(batchSize);
                total += batchSize;
            }
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
