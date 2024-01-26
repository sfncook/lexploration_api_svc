using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
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
            if(req==null || req.user_question==null){
                return BadRequest();
            }

            // Company company = await sharedQueriesService.GetCompanyById(companyid);
            // Conversation convo = await sharedQueriesService.GetConversationById(convoid);
            // if(company==null || convo==null) {
            //     return NotFound();
            // }


            // TODO: Add metric many contextDocs by company(?)
            float[] vector = await memoryStoreService.GetVector(req.user_question);
            string[] contextDocs = await memoryStoreService.GetRelevantContexts(vector, companyid);
            Console.WriteLine($"contextDocs:{contextDocs.Length}");
            Console.WriteLine(string.Join("','", contextDocs));

            string resp = await semanticKernelService.SubmitUserQuestion(req.user_question, contextDocs);

            return Ok(resp);
        }
    }

    public class SubmitRequest {
        public string user_question { get; set; }
        public bool mute { get; set; }
    }
}
