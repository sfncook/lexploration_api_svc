using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SalesBotApi.Models;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class AiController : Controller
    {
        private readonly SemanticKernelService semanticKernelService;
        private readonly MemoryStoreService memoryStoreService;

        public AiController(SemanticKernelService _semanticKernelService, MemoryStoreService _memoryStoreService)
        {
            semanticKernelService = _semanticKernelService;
            memoryStoreService = _memoryStoreService;
        }

        // *** ROOT ADMIN ***
        // GET: api/ai
        [HttpGet]
        [JwtAuthorize]
        public async Task<IActionResult> TestAi()
        {
            var resp = await memoryStoreService.Read("How will you help my business grow?");

            // AzureOpenAIEmbeddings openAIEmbeddings = new AzureOpenAIEmbeddings();
            // float[] resp = await openAIEmbeddings.GetEmbeddingsAsync("casino party");

            // AzureOpenAIEmbeddings azureOpenAIEmbeddings = new AzureOpenAIEmbeddings();
            // var resp = await azureOpenAIEmbeddings.GetEmbeddingsAsync("Hello world");
            // JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            // if(userData.role != "root") {
            //     return Unauthorized();
            // }

            // await memoryStoreService.Write();
            // await memoryStoreService.Read();

            // Response.Headers.Add("Access-Control-Allow-Origin", "*");
            // return Ok(await semanticKernelService.GetJoke());

            return Ok(resp);
        }
    }
}
