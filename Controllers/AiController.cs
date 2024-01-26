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
        private readonly AzureOpenAIEmbeddings azureOpenAIEmbeddings;
        private readonly WebpageProcessor webpageProcessor;

        public AiController(
            SemanticKernelService _semanticKernelService, 
            MemoryStoreService _memoryStoreService,
            AzureOpenAIEmbeddings _azureOpenAIEmbeddings,
            WebpageProcessor _webpageProcessor
        )
        {
            semanticKernelService = _semanticKernelService;
            memoryStoreService = _memoryStoreService;
            azureOpenAIEmbeddings = _azureOpenAIEmbeddings;
            webpageProcessor = _webpageProcessor;
        }

        // *** ROOT ADMIN ***
        // GET: api/ai
        [HttpGet]
        [JwtAuthorize]
        public async Task<IActionResult> TestAi()
        {
            // Query vector db
            // var resp = await memoryStoreService.Read("What is my name?");

            // Upsert to vector db
            // await memoryStoreService.Write("My name is Shawn", "https://example.com");

            // Chunker
            string[] chunks = await webpageProcessor.GetTextChunksFromUrlAsync("https://www.blacktiecasinoevents.com/", 1000);
            Console.WriteLine(string.Join("'********\n********'", chunks));

            // string content = "We sell brown horses, but no other colors of horses.";
            // var resp = await azureOpenAIEmbeddings.GetEmbeddingsAsync(content);

            return Ok();
        }
    }
}
