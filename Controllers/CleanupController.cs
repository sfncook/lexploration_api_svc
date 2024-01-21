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
    public class CleanupController : Controller
    {
        private readonly Container conversationsContainer;
        private readonly Container companiesContainer;
        private readonly Container chatbotsContainer;
        private readonly Container linksContainer;
        private readonly Container messagesContainer;
        private readonly Container usersContainer;
        private readonly SharedQueriesService queriesSvc;

        public CleanupController(CosmosDbService cosmosDbService, SharedQueriesService _queriesSvc)
        {
            conversationsContainer = cosmosDbService.ConversationsContainer;
            companiesContainer = cosmosDbService.CompaniesContainer;
            chatbotsContainer = cosmosDbService.ChatbotsContainer;
            linksContainer = cosmosDbService.LinksContainer;
            messagesContainer = cosmosDbService.MessagesContainer;
            usersContainer = cosmosDbService.UsersContainer;
            queriesSvc = _queriesSvc;
        }

        // DELETE: api/cleanup
        [HttpDelete]
        [JwtAuthorize]
        public async Task<IActionResult> PerformCleanup()
        {
            JwtPayload userData = HttpContext.Items["UserData"] as JwtPayload;
            string role = userData.role;
            if(role != "root") {
                return Unauthorized();
            }

            bool do_delete = true;
            IEnumerable<Conversation> conversations = await queriesSvc.GetAllItems<Conversation>(conversationsContainer);
            IEnumerable<Company> companies = await queriesSvc.GetAllItems<Company>(companiesContainer);
            IEnumerable<Chatbot> chatbots = await queriesSvc.GetAllItems<Chatbot>(chatbotsContainer);
            IEnumerable<Link> links = await queriesSvc.GetAllItems<Link>(linksContainer);
            IEnumerable<Message> messages = await queriesSvc.GetAllItems<Message>(messagesContainer);
            IEnumerable<UserWithJwt> users = await queriesSvc.GetAllItems<UserWithJwt>(usersContainer);

            HashSet<string> companyIdsWithUsers = new HashSet<string>();
            foreach (UserWithJwt user in users)
            {
                companyIdsWithUsers.Add(user.company_id);
            }
            foreach (var company in companies)
            {
                if (!companyIdsWithUsers.Contains(company.company_id.ToString()) &&
                    company.company_id.ToString()!="XXX" &&
                    company.company_id.ToString()!="all"
                )
                {
                    Console.WriteLine($"Deleting company with ID: {company.id}, Company ID: {company.company_id}");
                    if(do_delete) {
                        Console.WriteLine($"Deleting from Cosmos");
                        await companiesContainer.DeleteItemAsync<Company>(company.id, new PartitionKey(company.company_id));
                    }
                }
            }

            companies = await queriesSvc.GetAllItems<Company>(companiesContainer);

            HashSet<string> companyIds = new HashSet<string>();
            foreach (Company company in companies)
            {
                companyIds.Add(company.company_id);
            }

            foreach (var convo in conversations)
            {
                if (!companyIds.Contains(convo.company_id.ToString()))
                {
                    Console.WriteLine($"Deleting convo with ID: {convo.id}, Company ID: {convo.company_id}");
                    if(do_delete) {
                        Console.WriteLine($"Deleting from Cosmos");
                        await conversationsContainer.DeleteItemAsync<Conversation>(convo.id, new PartitionKey(convo.id));
                    }
                }
            }

            foreach (var chatbot in chatbots)
            {
                if (!companyIds.Contains(chatbot.company_id.ToString()))
                {
                    Console.WriteLine($"Deleting chatbots with ID: {chatbot.id}, Company ID: {chatbot.company_id}");
                    if(do_delete) {
                        Console.WriteLine($"Deleting from Cosmos");
                        await chatbotsContainer.DeleteItemAsync<Chatbot>(chatbot.id, new PartitionKey(chatbot.company_id));
                    }
                }
            }

            foreach (var link in links)
            {
                if (!companyIds.Contains(link.company_id.ToString()))
                {
                    Console.WriteLine($"Deleting link with ID: {link.id}, Company ID: {link.company_id}");
                    if(do_delete) {
                        Console.WriteLine($"Deleting from Cosmos");
                        await linksContainer.DeleteItemAsync<Link>(link.id, new PartitionKey(link.company_id));
                    }
                }
            }

            IEnumerable<Conversation> remainingConversations = await queriesSvc.GetAllItems<Conversation>(conversationsContainer);
            HashSet<string> remainingConversationIds = new HashSet<string>();
            foreach (Conversation convo in remainingConversations)
            {
                remainingConversationIds.Add(convo.id);
            }


            foreach (var msg in messages)
            {
                if (!remainingConversationIds.Contains(msg.conversation_id.ToString()))
                {
                    Console.WriteLine($"Deleting msg with ID: {msg.id}, Conversation ID: {msg.conversation_id}");
                    if(do_delete) {
                        Console.WriteLine($"Deleting from Cosmos");
                        await messagesContainer.DeleteItemAsync<Message>(msg.id, new PartitionKey(msg.conversation_id));
                    }
                }
            }
            return new OkResult();
        }
    }
}
