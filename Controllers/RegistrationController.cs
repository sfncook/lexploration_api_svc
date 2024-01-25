using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using System;

namespace SalesBotApi.Controllers
{

    [Route("api/[controller]")] 
    [ApiController]
    public class RegistrationController : Controller
    {

        private readonly Container usersContainer;
        private readonly SharedQueriesService queriesSvc;
        private readonly EmailService emailService;

        public RegistrationController(
            CosmosDbService cosmosDbService,
            SharedQueriesService _queriesSvc,
            EmailService _emailService
        )
        {
            usersContainer = cosmosDbService.UsersContainer;
            queriesSvc = _queriesSvc;
            emailService = _emailService;
        }

        // POST: api/register/test
        [HttpPost("test")]
        public async Task<IActionResult> Test([FromBody] LoginRequest loginReq)
        {
            await emailService.SendEmail(
                "hello@saleschat.bot", 
                "Sales Chatbot Registration", 
                "sfncook@gmail.com", 
                "Hello from C#", 
                "Hello world!  This is from C# code."
            );
            return Ok();
        }

        // POST: api/register
        [HttpPost]
        public async Task<IActionResult> RegisterNewUser([FromBody] LoginRequest loginReq)
        {
            if (loginReq.user_name == null || loginReq.password == null)
            {
                return BadRequest("Invalid request, missing parameters");
            }

            IEnumerable<UserWithJwt> users = await queriesSvc.GetAllItems<UserWithJwt>(usersContainer);
            foreach (UserWithJwt preexistingUser in users)
            {
                if (preexistingUser.user_name.ToLower() == loginReq.user_name.ToLower())
                {
                    return BadRequest("User exists");
                }
            }

            string newUuid = Guid.NewGuid().ToString();
            string companyId = "XXX";
            UserWithPassword newUser = new UserWithPassword
            {
                id = newUuid,
                user_name = loginReq.user_name,
                password = loginReq.password,
                company_id = companyId,
                role = "company_owner",
                approval_status = ""
            };

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            await usersContainer.CreateItemAsync(newUser, new PartitionKey(companyId));
            await emailService.SendRegistrationEmail(loginReq.user_name);

            return NoContent();
        }
    }
}
