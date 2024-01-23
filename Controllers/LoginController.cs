using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;

namespace SalesBotApi.Controllers
{
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
        public async Task<ActionResult<UserWithJwt>> LoginUser([FromBody] LoginRequest loginReq)
        {
            string sqlQueryText = $"SELECT * FROM c WHERE c.user_name = '{loginReq.user_name}' AND c.password = '{loginReq.password}' OFFSET 0 LIMIT 1";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            UserWithJwt authorizedUser = null;
            try
            {
                using (FeedIterator<UserWithJwt> feedIterator = usersContainer.GetItemQueryIterator<UserWithJwt>(queryDefinition))
                {
                    if (feedIterator.HasMoreResults)
                    {
                        FeedResponse<UserWithJwt> response = await feedIterator.ReadNextAsync();
                        authorizedUser = response.First();
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
