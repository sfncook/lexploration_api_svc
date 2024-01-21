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

namespace SalesBotApi.Controllers
{

    [Route("api/[controller]")] 
    [ApiController]
    public class RegistrationController : Controller
    {

        private readonly Container usersContainer;
        private readonly SharedQueriesService queriesSvc;

        public RegistrationController(
            CosmosDbService cosmosDbService,
            SharedQueriesService _queriesSvc
        )
        {
            usersContainer = cosmosDbService.UsersContainer;
            queriesSvc = _queriesSvc;
        }

        // POST: api/register
        [HttpPost]
        public async Task<IActionResult> LoginUser([FromBody] LoginRequest loginReq)
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
            return NoContent();
        }
    }
}
