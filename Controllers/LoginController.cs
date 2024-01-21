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
                authorizedUser.jwt = CreateToken(authorizedUser, "foo-bar-001", TimeSpan.FromHours(24));
                return Ok(authorizedUser);
            }
            else return Unauthorized();
        }

        private string CreateToken(AuthorizedUser authorizedUser, string secret, TimeSpan tokenLifetime)
        {
            // Header
            var header = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";
            var headerBytes = Encoding.UTF8.GetBytes(header);
            var headerBase64 = Convert.ToBase64String(headerBytes);

            JwtPayload payload = new JwtPayload
            {
                user_id = authorizedUser.id,
                user_name = authorizedUser.user_name,
                company_id = authorizedUser.company_id,
                exp = DateTimeOffset.UtcNow.Add(tokenLifetime).ToUnixTimeSeconds()
            };
            string payloadStr = JsonConvert.SerializeObject(payload);
            var payloadBytes = Encoding.UTF8.GetBytes(payloadStr);
            var payloadBase64 = Convert.ToBase64String(payloadBytes);

            // Signature
            var signature = $"{headerBase64}.{payloadBase64}";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signature));
                var signatureBase64 = Convert.ToBase64String(signatureBytes);

                return $"{headerBase64}.{payloadBase64}.{signatureBase64}";
            }
        }
    }
}
