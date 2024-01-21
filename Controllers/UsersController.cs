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
    public class ApprovalStatusResponse {
        public string approval_status { get; set; }
    }

    [Route("api/[controller]")] 
    [ApiController]
    public class UsersController : Controller
    {

        private readonly Container usersContainer;

        public UsersController(CosmosDbService cosmosDbService)
        {
            usersContainer = cosmosDbService.UsersContainer;
        }

        // GET: api/users/approval_status
        [HttpGet("approval_status")]
        [JwtAuthorize]
        public async Task<ActionResult<ApprovalStatusResponse>> GetUserApprovalStatus()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string user_id = userData.id;
            string sqlQueryText = $"SELECT * FROM c WHERE c.id = '{user_id}'";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            UserBase user = null;
            try
            {
                using (FeedIterator<UserBase> feedIterator = usersContainer.GetItemQueryIterator<UserBase>(queryDefinition))
                {
                    if (feedIterator.HasMoreResults)
                    {
                        FeedResponse<UserBase> response = await feedIterator.ReadNextAsync();
                        user = response.First();
                    }
                }
            }
            catch (CosmosException ex)
            {
                return Unauthorized();
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            if(user != null) {
                ApprovalStatusResponse resp = new ApprovalStatusResponse
                {
                    approval_status = user.approval_status
                };
                return Ok(resp);
            }
            else return Unauthorized();
        }

        // *** ROOT ADMIN ***
        // GET: api/users
        [HttpGet]
        [JwtAuthorize]
        public async Task<ActionResult<IEnumerable<UserBase>>> GetAllUsers()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            if(userData.role != "root") {
                return Unauthorized();
            }

            Response.Headers.Add("Access-Control-Allow-Origin", "*");

            string sqlQueryText = "SELECT * FROM c";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            List<UserBase> users = new List<UserBase>();
            using (FeedIterator<UserBase> feedIterator = usersContainer.GetItemQueryIterator<UserBase>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<UserBase> response = await feedIterator.ReadNextAsync();
                    users.AddRange(response.ToList());
                }
            }
            return users;
        }
    }
}
