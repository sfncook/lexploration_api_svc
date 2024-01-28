using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using static OpenAiHttpRequestService;
using System.Reflection.Metadata.Ecma335;
using System.Linq;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class AiController : Controller
    {
        private readonly OpenAiHttpRequestService openAiHttpRequestService;
        private readonly MemoryStoreService memoryStoreService;
        private readonly SharedQueriesService sharedQueriesService;

        public AiController(
            OpenAiHttpRequestService _openAiHttpRequestService, 
            MemoryStoreService _memoryStoreService,
            SharedQueriesService _sharedQueriesService
        )
        {
            openAiHttpRequestService = _openAiHttpRequestService;
            memoryStoreService = _memoryStoreService;
            sharedQueriesService = _sharedQueriesService;
        }

        // POST: api/ai/submit_user_question
        [HttpPost("submit_user_question")]
        public async Task<IActionResult> SubmitUserQuestion(
            [FromBody] SubmitRequest req,
            [FromQuery] string companyid,
            [FromQuery] string convoid
        )
        {
            if(req==null || req.user_question==null){
                return BadRequest();
            }


            Company company = null;
            Conversation convo = null;
            Chatbot chatbot = null;
            IEnumerable<Models.Message> messages = null;
            IEnumerable<Refinement> refinements = null;
            float[] vector = null;

            try
            {
                var companyTask = sharedQueriesService.GetCompanyById(companyid);
                var convoTask = sharedQueriesService.GetConversationById(convoid);
                var chatbotTask = sharedQueriesService.GetFirstChatbotByCompanyId(companyid);
                var refinementsTask = sharedQueriesService.GetRefinementsByCompanyId(companyid);
                var msgsTask = sharedQueriesService.GetRecentMsgsForConvo(convoid);
                var vectorTask = memoryStoreService.GetVector(req.user_question);

                await Task.WhenAll(companyTask, convoTask, chatbotTask, vectorTask);

                // After all tasks are complete, you can assign the results
                company = await companyTask;
                convo = await convoTask;
                chatbot = await chatbotTask;
                messages = await msgsTask;
                refinements = await refinementsTask;
                vector = await vectorTask;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            // TODO: Add metric many contextDocs by company(?)
            string[] contextDocs = await memoryStoreService.GetRelevantContexts(vector, companyid);
            Console.WriteLine($"contextDocs:{contextDocs.Length}");
            Console.WriteLine(string.Join("','", contextDocs));

            GptMessage[] gptMessages = convertCosmosMessagesToGptFormat(messages);

            ChatCompletionResponse chatCompletionResponse = await openAiHttpRequestService.SubmitUserQuestion(
                req.user_question, 
                contextDocs, 
                company, 
                convo, 
                chatbot, 
                refinements,
                gptMessages
            );

            return Ok(chatCompletionResponse);
        }

        // def convert_cosmos_messages_to_gpt_format(messages):
        // converted_messages = []

        // for message in messages:
        //     user_message = {
        //         "role": "user",
        //         "content": message["user_msg"]
        //     }
        //     assistant_message = {
        //         "role": "assistant",
        //         "content": message["assistant_response"]
        //     }

        //     converted_messages.append(user_message)
        //     converted_messages.append(assistant_message)

        // return converted_messages
        private GptMessage[] convertCosmosMessagesToGptFormat(IEnumerable<Models.Message> messages) {
            GptMessage[] gptMessages = new GptMessage[messages.Count() *2];
            foreach(Models.Message msg in messages){
                GptMessage usrMsg = new GptMessage{
                    role = "role",
                    content = msg.user_msg
                };
                GptMessage assistantMsg = new GptMessage{
                    role = "assistant",
                    content = msg.assistant_response
                };
                gptMessages.Append(usrMsg);
                gptMessages.Append(assistantMsg);
            }
            return gptMessages;
        }
    }
    

    public class SubmitRequest {
        public string user_question { get; set; }
        public bool mute { get; set; }
    }

    public class GptMessage {
        public string role { get; set; }
        public string content { get; set; }
    }
}
