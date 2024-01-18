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
using System.Text;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class TodoController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public TodoController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        // PUT: api/todo/email
        [HttpPost("email")]
        public async Task<IActionResult> PutTodoItem()
        {
            Console.WriteLine("Sending an email now");


            EmailRequest emailReq = new EmailRequest
            {
                recipient_email = "sfncook@gmail.com",
                subject = "Test Email2",
                body = "This is another test email sent from C# script."
            };

            HttpClient client = _clientFactory.CreateClient();
            string postUrl = "https://salesbot-001.azurewebsites.net/api/email";
//            string postUrl = "http://localhost:7071/api/email";
            string body = JsonConvert.SerializeObject(emailReq);
            var postResponse = await client.PostAsync(postUrl, new StringContent(body, Encoding.UTF8, "application/json"));

            Console.WriteLine("Done");
            return NoContent();
        }
    }
}
