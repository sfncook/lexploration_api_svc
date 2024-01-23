using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SalesBotApi.Models;
using System;
using Microsoft.SemanticKernel;

namespace SalesBotApi.Controllers
{
    public class EmailPlugin
    {
        [KernelFunction]
        [Description("Sends an email to a recipient.")]
        public async Task SendEmailAsync(
            Kernel kernel,
            [Description("Semicolon delimitated list of emails of the recipients")] string recipientEmails,
            string subject,
            string body
        )
        {
            // Add logic to send an email using the recipientEmails, subject, and body
            // For now, we'll just print out a success message to the console
            Console.WriteLine("Email sent!");
        }
    }

    [Route("api/[controller]")] 
    [ApiController]
    public class AiController : Controller
    {

        // *** ROOT ADMIN ***
        // GET: api/ai
        [HttpGet]
        [JwtAuthorize]
        public async Task<IActionResult> TestAi()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            if(userData.role != "root") {
                return Unauthorized();
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return Ok();
        }
    }
}
