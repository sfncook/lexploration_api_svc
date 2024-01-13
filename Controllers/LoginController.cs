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
        private static readonly User[] Users = new[]
        {
            new User { Username = "sfncook@gmail.com", Password = "password", company_id="all"},
            new User { Username = "nick@njpconsultingllc.com", Password = "password", company_id="all"},
            new User { Username = "wowsers@gmail.com", Password = "password", company_id="all"},
            new User { Username = "dave@blacktiecasinoevents.com", Password = "password", company_id="blacktiecasinoevents" }
        };


        // POST: api/login
        [HttpPost]
        public ActionResult<AuthorizedUser> LoginUser([FromBody] LoginRequest loginReq)
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "*");

            // Check if the username and password match any user in the array
            var user = Users.FirstOrDefault(u => u.Username == loginReq.user_name && u.Password == loginReq.password);

            if (user != null)
            {
                // User found - create and return an AuthorizedUser object
                var authorizedUser = new AuthorizedUser {
                    user_name = user.Username,
                    company_id = user.company_id
                };
                return Ok(authorizedUser);
            }
            else
            {
                // User not found or password does not match
                return Unauthorized();
            }
        }
    }
}
