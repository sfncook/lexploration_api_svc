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
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace SalesBotApi.Controllers
{
    class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string company_id { get; set; }
    }

    [Route("api/[controller]")] 
    [ApiController]
    public class LoginController : Controller
    {

        private readonly Container usersContainer;

        public LoginController(CosmosDbService cosmosDbService)
        {
            usersContainer = cosmosDbService.UsersContainer;
        }

        // POST: api/login
        [HttpPost]
        public async Task<ActionResult<AuthorizedUser>> LoginUser([FromBody] LoginRequest loginReq)
        {
            string sqlQueryText = $"SELECT * FROM c WHERE c.user_name = '{loginReq.user_name}' AND c.password = '{loginReq.password}' OFFSET 0 LIMIT 1";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            AuthorizedUser authorizedUser = null;
            try
            {
                using (FeedIterator<AuthorizedUser> feedIterator = usersContainer.GetItemQueryIterator<AuthorizedUser>(queryDefinition))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        FeedResponse<AuthorizedUser> response = await feedIterator.ReadNextAsync();
                        authorizedUser = response.First();
                        break;
                    }
                }
            }
            catch (CosmosException ex)
            {
                return Unauthorized();
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");

            if(authorizedUser != null) {
                authorizedUser.jwt = JwtService.CreateToken(authorizedUser);
                return Ok(authorizedUser);
            }
            else return Unauthorized();
        }
    }
}
