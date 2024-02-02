using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using static LogBufferService;
using System.Collections.Generic;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class LogsController : Controller
    {

        private readonly Container logsContainer;

        public LogsController(CosmosDbService cosmosDbService)
        {
            logsContainer = cosmosDbService.LogsContainer;
        }

        // GET: api/logs
        [HttpGet]
        [JwtAuthorize]
        public async Task<ActionResult<List<LogMsg>>> GetLogs(
            [FromQuery] string logLevelStr,
            [FromQuery] int offset,
            [FromQuery] int limit
        ) {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string role = userData.role;
            if(role != "root") {
                return Unauthorized();
            }

            if(offset == 0 && limit ==0) {
                return BadRequest();
            }

            string sqlQueryText = $"SELECT * FROM c WHERE OFFSET {offset} LIMIT {limit}";
            if(logLevelStr != null) {
                sqlQueryText = $"SELECT * FROM c WHERE c.levelStr = '{logLevelStr}' OFFSET {offset} LIMIT {limit}";
            }
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            List<LogMsg> logMsgs = new List<LogMsg>();
            using (FeedIterator<LogMsg> feedIterator = logsContainer.GetItemQueryIterator<LogMsg>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<LogMsg> response = await feedIterator.ReadNextAsync();
                    logMsgs.AddRange(response.ToList());
                }
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return logMsgs;
        }
    }
}
