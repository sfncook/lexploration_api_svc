using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using static OpenAiHttpRequestService;
using static AzureSpeechService;
using Newtonsoft.Json;

namespace SalesBotApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class AiController : Controller
    {
        private readonly OpenAiHttpRequestService openAiHttpRequestService;
        private readonly MemoryStoreService memoryStoreService;
        private readonly SharedQueriesService sharedQueriesService;
        private readonly Container messagesContainer;
        private readonly AzureSpeechService azureSpeechService;

        public AiController(
            OpenAiHttpRequestService _openAiHttpRequestService, 
            MemoryStoreService _memoryStoreService,
            SharedQueriesService _sharedQueriesService,
            CosmosDbService cosmosDbService,
            AzureSpeechService azureSpeechService
        )
        {
            openAiHttpRequestService = _openAiHttpRequestService;
            memoryStoreService = _memoryStoreService;
            sharedQueriesService = _sharedQueriesService;
            messagesContainer = cosmosDbService.MessagesContainer;
            this.azureSpeechService = azureSpeechService;
        }

        // PUT: api/ai/submit_user_question
        [HttpPut("submit_user_question")]
        public async Task<ActionResult<SubmitResponse>> SubmitUserQuestion(
            [FromBody] SubmitRequest req,
            [FromQuery] string companyid,
            [FromQuery] string convoid
        )
        {
            if(req==null || req.user_msg==null){
                return BadRequest();
            }


            Company company = null;
            Conversation convo = null;
            Chatbot chatbot = null;
            IEnumerable<Message> messages = null;
            IEnumerable<Refinement> refinements = null;
            float[] vector = null;

            try
            {
                var companyTask = sharedQueriesService.GetCompanyById(companyid);
                var convoTask = sharedQueriesService.GetConversationById(convoid);
                var chatbotTask = sharedQueriesService.GetFirstChatbotByCompanyId(companyid);
                var refinementsTask = sharedQueriesService.GetRefinementsByCompanyId(companyid);
                var msgsTask = sharedQueriesService.GetRecentMsgsForConvo(convoid, 4);
                var vectorTask = memoryStoreService.GetVector(req.user_msg);

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
            // Console.WriteLine(string.Join("','", contextDocs));

            // TODO: Add metric latency call
            AssistantResponse assistantResponse = await openAiHttpRequestService.SubmitUserQuestion(
                req.user_msg, 
                contextDocs, 
                company, 
                convo, 
                chatbot, 
                refinements,
                messages
            );

            SpeechResults speechResults;
            if(!req.mute) {
                speechResults = await azureSpeechService.GetSpeech(assistantResponse.assistant_response);
                // Console.WriteLine(JsonConvert.SerializeObject(speechResults));
            } else {
                speechResults = new SpeechResults() {
                    lipsync = new LipSyncResults(),
                    audio = ""
                };
            }

            await InsertNewMessage(
                convoid,
                companyid,
                req.user_msg,
                assistantResponse
            );

            //TODO: Send email

            SubmitResponse submitResponse = new SubmitResponse() {
                assistant_response = new SubmitResponseAssistantResponse(){role="assistant", content=assistantResponse.assistant_response},
                redirect_url = assistantResponse.redirect_url,
                lipsync = speechResults.lipsync,
                audio = speechResults.audio
            };

            return Ok(submitResponse);
        }

        private async Task InsertNewMessage(
            string convoid,
            string company_id,
            string user_msg,
            AssistantResponse assistantResponse
        ) {
            Message message = new Message {
                id = Guid.NewGuid().ToString(),
                conversation_id = convoid,
                user_msg = user_msg,
                assistant_response = assistantResponse.assistant_response,
                company_id = company_id,
                user_wants_to_be_contacted = assistantResponse.user_wants_to_be_contacted,
                user_wants_to_install_the_demo = assistantResponse.user_wants_to_install_the_demo,
                user_wants_to_schedule_call_with_sales_rep = assistantResponse.user_wants_to_schedule_call_with_sales_rep,
                user_first_name = assistantResponse.user_first_name,
                user_last_name = assistantResponse.user_last_name,
                user_email = assistantResponse.user_email,
                user_phone_number = assistantResponse.user_phone_number,
                redirect_url = assistantResponse.redirect_url
            };
            await messagesContainer.CreateItemAsync(message, new PartitionKey(convoid));
        }

    }//class AiController
    
    public class SubmitResponse {
        public SubmitResponseAssistantResponse assistant_response { get; set; }
        public string redirect_url { get; set; }
        public LipSyncResults lipsync { get; set; }
        public string audio { get; set; }
    }
    public class SubmitResponseAssistantResponse {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class SubmitRequest {
        public string user_msg { get; set; }
        public bool mute { get; set; }
    }

    public class GptMessage {
        public string role { get; set; }
        public string content { get; set; }
    }
}
