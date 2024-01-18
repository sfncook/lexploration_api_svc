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

        public RegistrationController(CosmosDbService cosmosDbService)
        {
            usersContainer = cosmosDbService.UsersContainer;
        }

        // POST: api/register
        [HttpPost]
        public async Task<IActionResult> LoginUser([FromBody] LoginRequest loginReq)
        {
            if (loginReq.user_name == null || loginReq.password == null)
            {
                return BadRequest("Invalid request, missing parameters");
            }

            string newUuid = Guid.NewGuid().ToString();
            string companyId = "XXX";
            NewUser newUser = new NewUser
            {
                id = newUuid,
                user_name = loginReq.user_name,
                password = loginReq.password,
                company_id = companyId
            };

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            await usersContainer.CreateItemAsync(newUser, new PartitionKey(companyId));
            return NoContent();
        }
    }
}
