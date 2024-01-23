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

        public AiController(SemanticKernelService _semanticKernelService)
        {
            semanticKernelService = _semanticKernelService;
        }

        // *** ROOT ADMIN ***
        // GET: api/ai
        [HttpGet]
        [JwtAuthorize]
        public async Task<ActionResult<string>> TestAi()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            if(userData.role != "root") {
                return Unauthorized();
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return Ok(await semanticKernelService.GetJoke());
        }
    }
}
