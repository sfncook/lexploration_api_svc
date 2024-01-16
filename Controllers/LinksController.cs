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
    public class LinksController : Controller
    {
        private readonly Container linksContainer;

        public LinksController(CosmosDbService cosmosDbService)
        {
            linksContainer = cosmosDbService.LinksContainer;
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

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return links;
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
        public async Task<ActionResult<Link>> AddLink([FromQuery] string company_id, [FromBody] AddLinkRequest addLinkRequest)
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
        }
    }
}
