using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SalesBotApi.Models;
using System;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class AiController : Controller
    {
        private readonly SemanticKernelService semanticKernelService;
        private readonly MemoryStoreService memoryStoreService;
        private readonly SharedQueriesService sharedQueriesService;

        public AiController(
            SemanticKernelService _semanticKernelService, 
            MemoryStoreService _memoryStoreService,
            SharedQueriesService _sharedQueriesService
        )
        {
            semanticKernelService = _semanticKernelService;
            memoryStoreService = _memoryStoreService;
            sharedQueriesService = _sharedQueriesService;
        }

        // POST: api/ai/submit_user_quesiton
        [HttpPost("submit_user_quesiton")]
        public async Task<IActionResult> SubmitUserQuestion(
            [FromBody] SubmitRequest req,
            [FromQuery] string companyid
        )
        {
            if(req==null || req.user_msg==null){
                return BadRequest();
            }

            // Company company = await sharedQueriesService.GetCompanyById(companyid);
            // Conversation convo = await sharedQueriesService.GetConversationById(convoid);
            // if(company==null || convo==null) {
            //     return NotFound();
            // }


            // var contextDocs = await memoryStoreService.Read(req.user_msg);
            string[] contextDocs = await memoryStoreService.GetRelevantContexts(req.user_msg, companyid);
            Console.WriteLine($"contextDocs:{contextDocs.Length}");

            return Ok(contextDocs);
        }

        // *** ROOT ADMIN ***
        // GET: api/ai
        [HttpGet]
        [JwtAuthorize]
        public async Task<IActionResult> TestAi()
        {
            // Query vector db
            var resp = await memoryStoreService.Read("What is my name?");

            // Upsert to vector db
            // await memoryStoreService.Write("My name is Shawn", "https://example.com");

            // Chunker
            // string[] chunks = await webpageProcessor.GetTextChunksFromUrlAsync("https://www.blacktiecasinoevents.com/", 1000);
            // Console.WriteLine(string.Join("'********\n********'", chunks));

            // string content = "We sell brown horses, but no other colors of horses.";
            // var resp = await azureOpenAIEmbeddings.GetEmbeddingsAsync(content);

            return Ok();
        }
    }

    public class SubmitRequest {
        public string user_msg { get; set; }
        public bool mute { get; set; }
    }
}
