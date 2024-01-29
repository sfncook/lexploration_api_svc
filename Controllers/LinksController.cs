using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using System;
using System.Text;

namespace SalesBotApi.Controllers
{
    public class AddLinkRequest
    {
        public string link { get; set; }
    }
    public class StartTrainingRequest
    {
        public string[] links { get; set; }
    }
    public class DeleteLinkRequest
    {
        public string link_id { get; set; }
        public string company_id { get; set; }
    }

    [Route("api/[controller]")] 
    [ApiController]
    public class LinksController : Controller
    {
        private readonly Container linksContainer;
        private readonly IHttpClientFactory _clientFactory;
        private readonly Container companiesContainer;
        private readonly QueueService queueService;
        private readonly SharedQueriesService sharedQueriesService;
        private readonly InMemoryCacheService<Company> cacheCompany;

        public LinksController(
            CosmosDbService cosmosDbService,
            IHttpClientFactory clientFactory,
            QueueService _queueService,
            SharedQueriesService sharedQueriesService,
            InMemoryCacheService<Company> cacheCompany
        )
        {
            linksContainer = cosmosDbService.LinksContainer;
            _clientFactory = clientFactory;
            companiesContainer = cosmosDbService.CompaniesContainer;
            queueService = _queueService;
            this.sharedQueriesService = sharedQueriesService;
            this.cacheCompany = cacheCompany;
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

        //DELETE THIS
        // POST: api/links/add
        [HttpPost("add")]
        [JwtAuthorize]
        public async Task<IActionResult> AddLinkToDb([FromBody] AddLinkRequest req)
        {
            if (req == null)
            {
                return BadRequest("Missing link text in body");
            }

            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;

            Link newLink = new Link
            {
                id = Guid.NewGuid().ToString(),
                link = req.link,
                company_id = company_id,
                status = "",
                result = "",
            };

            try {
                await linksContainer.CreateItemAsync(newLink, new PartitionKey(company_id));
            }
            catch (CosmosException)
            {
                return BadRequest();
            }
            return NoContent();
        }

        //DELETE THIS
        // POST: api/links/remove
        [HttpDelete("remove")]
        [JwtAuthorize]
        public async Task<IActionResult> RemoveLinkFromDb([FromBody] DeleteLinkRequest req)
        {
            if (req == null)
            {
                return BadRequest("Missing link text in body");
            }

            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string company_id = userData.company_id;

            try {
                await linksContainer.DeleteItemAsync<Company>(req.link_id, new PartitionKey(req.company_id));
            }
            catch (CosmosException)
            {
                return BadRequest();
            }
            return NoContent();
        }


        //DELETE THIS
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

        //DELETE THIS
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
                    queueService.EnqueueScrapLinksMessageAsync(JsonConvert.SerializeObject(link)))
                    .ToList();
                await Task.WhenAll(tasks);
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return NoContent();
        }
        
        // POST: api/links/start_training
        [HttpPost("start_training")]
        [JwtAuthorize]
        public async Task<IActionResult> StartTraining([FromBody] StartTrainingRequest req)
        {
            if (req == null || req.links == null || !req.links.Any())
            {
                return BadRequest();
            }

            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            if (userData == null)
            {
                return Unauthorized(); // Or some other appropriate response
            }

            string company_id = userData.company_id;
            if (company_id == "all" || company_id == "XXX")
            {
                return BadRequest();
            }

            await SetCompanyTraining(company_id);

            var tasks = req.links.Select(linkStr => AddLinkToDb2(linkStr, company_id)).ToList();
            await Task.WhenAll(tasks);

            IEnumerable<Link> allLinks = await GetAllLinksForCompany(company_id);
            var filteredLinks = allLinks.Where(link => string.IsNullOrWhiteSpace(link.status));
            int batchSize = 10;
            var linkBatches = BatchLinks(filteredLinks, batchSize);

            foreach (var batch in linkBatches)
            {
                var tasks2 = batch.Select(link =>
                    queueService.EnqueueScrapLinksMessageAsync(JsonConvert.SerializeObject(link)))
                    .ToList();
                await Task.WhenAll(tasks2);
            }

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

        private async Task AddLinkToDb2(string linkStr, string company_id) {
            Link newLink = new Link
            {
                id = Guid.NewGuid().ToString(),
                link = linkStr,
                company_id = company_id,
                status = "",
                result = "",
            };

            await linksContainer.CreateItemAsync(newLink, new PartitionKey(company_id));
        }

        private async Task<ItemResponse<dynamic>> SetCompanyTraining(string company_id) {
            Company company = await sharedQueriesService.GetCompanyById(company_id);
            List<PatchOperation> patchOperations = new List<PatchOperation>()
            {
                PatchOperation.Add("/training", true)
            };
            var resp = await companiesContainer.PatchItemAsync<dynamic>(company.id, new PartitionKey(company_id), patchOperations);
            cacheCompany.Clear(company_id);
            return resp;
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
