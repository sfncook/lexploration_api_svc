using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;

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

        // POST: api/ai/submit_user_question
        [HttpPost("submit_user_question")]
        public async Task<IActionResult> SubmitUserQuestion(
            [FromBody] SubmitRequest req,
            [FromQuery] string companyid,
            [FromQuery] string convoid
        )
        {
            if(req==null || req.user_question==null){
                return BadRequest();
            }


            Company company = null;
            Conversation convo = null;
            Chatbot chatbot = null;
            float[] vector = null;

            try
            {
                var companyTask = sharedQueriesService.GetCompanyById(companyid);
                var convoTask = sharedQueriesService.GetConversationById(convoid);
                var chatbotTask = sharedQueriesService.GetFirstChatbotByCompanyId(companyid);
                var vectorTask = memoryStoreService.GetVector(req.user_question);

                await Task.WhenAll(companyTask, convoTask, chatbotTask, vectorTask);

                // After all tasks are complete, you can assign the results
                company = await companyTask;
                convo = await convoTask;
                chatbot = await chatbotTask;
                vector = await vectorTask;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            // TODO: Add metric many contextDocs by company(?)
            string[] contextDocs = await memoryStoreService.GetRelevantContexts(vector, companyid);
            Console.WriteLine($"contextDocs:{contextDocs.Length}");
            Console.WriteLine(string.Join("','", contextDocs));

            string resp = await semanticKernelService.SubmitUserQuestion(req.user_question, contextDocs, company, convo, chatbot);

            return Ok(resp);
        }
    }

    public class SubmitRequest {
        public string user_question { get; set; }
        public bool mute { get; set; }
    }
}
