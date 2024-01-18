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
using System.Net;
using System.Net.Mail;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class TodoController : Controller
    {

        // PUT: api/Todo/email
        [HttpPut("email")]
        public async Task<IActionResult> PutTodoItem()
        {
            Console.WriteLine("Sending an email now");

            var fromAddress = new MailAddress("james@saleschat.bot", "Sales Chatbot");
            const string fromPassword = "n$6d2%{e|@71";
            var toAddress = new MailAddress("sfncook@gmail.com");

            var smtp = new SmtpClient
            {
                Host = "mail.saleschat.bot",
                Port = 465,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("hello@saleschat.bot",  "n$6d2%{e|@71")
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = "Hello world!",
                Body = "Hi, this is a message from an App Server."
            })
            {
                smtp.Send(message);
            }

            Console.WriteLine("Done");
            return NoContent();
        }
    }
}
