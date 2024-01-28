using System.Threading.Tasks;
using System;
using SalesBotApi.Models;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;

public class OpenAiHttpRequestService
{

    private readonly HttpClient _httpClient;
    public OpenAiHttpRequestService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<ChatCompletionResponse> SubmitUserQuestion(
        string userQuestion, 
        string[] contextDocs,
        Company company,
        Conversation convo,
        Chatbot chatbot,
        IEnumerable<Refinement> refinements,
        SalesBotApi.Controllers.GptMessage[] gptMessages
    )
    {
        PromptBuilder promptBuilder = new PromptBuilder();
        string prompt = promptBuilder
            .setUserQuestion(userQuestion)
            .setCompany(company)
            .setChatbot(chatbot)
            .setContextDocs(contextDocs)
            .setConversation(convo)
            .setRefinements(refinements)
            .build();

        OpenAiRequestBuilder openAiRequestBuilder = new OpenAiRequestBuilder();
        string reqParams = openAiRequestBuilder
            .setModel("gpt-3.5-turbo")
            .setUserQuestion(userQuestion)
            .setSystemPrompt(prompt)
            // .setSystemPrompt("You are a helpful assistant")
            .build();

        reqParams = EscapeStringForJson(reqParams);
        Console.WriteLine(reqParams);

        var content = new StringContent(reqParams, Encoding.UTF8, "application/json");

        // Replace HttpMethod.Get with HttpMethod.Post
        using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions"))
        {
            requestMessage.Content = content;
            requestMessage.Headers.Add("Authorization", "Bearer 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");

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            string responseString = await response.Content.ReadAsStringAsync();
            ChatCompletionResponse chatCompletionResponse = JsonConvert.DeserializeObject<ChatCompletionResponse>(responseString);
            string argumentsStr = chatCompletionResponse.choices[0].message.tool_calls[0].function.arguments;
            chatCompletionResponse.choices[0].message.tool_calls[0].function.assistantResponse = JsonConvert.DeserializeObject<AssistantResponse>(argumentsStr);

            return chatCompletionResponse;
        }
    }

    public class FunctionDef {
        public string name { get; set;}
        public string description { get; set;}
        public AssistantResponse parameters { get; set;}
    }

// {
//   "id": "chatcmpl-8m73YophFst40PfCCz4IBs0PkmEGs",
//   "object": "chat.completion",
//   "created": 1706477560,
//   "model": "gpt-3.5-turbo-0613",
//   "choices": [
//     {
//       "index": 0,
//       "message": {
//         "role": "assistant",
//         "content": null,
//         "tool_calls": [
//           {
//             "id": "call_JCPMlEmCPufRREZr6iRt7xqr",
//             "type": "function",
//             "function": {
//               "name": "response_with_optional_user_data",
//               "arguments": "{\n  \"assistant_response\": \"Nice to meet you, Shawn! How can I assist you today?\",\n  \"user_first_name\": \"Shawn\",\n  \"user_email\": \"sfncook@gmail.com\"\n}"
//             }
//           }
//         ]
//       },
//       "logprobs": null,
//       "finish_reason": "stop"
//     }
//   ],
//   "usage": {
//     "prompt_tokens": 307,
//     "completion_tokens": 44,
//     "total_tokens": 351
//   },
//   "system_fingerprint": null
// }

    public class ChatCompletionResponse
    {
        public string id { get; set; }
        public string @object { get; set; }
        public long created { get; set; }
        public string model { get; set; }
        public List<Choice> choices { get; set; }
        public Usage usage { get; set; }
        public object system_fingerprint { get; set; }
    }

    public class Choice
    {
        public int index { get; set; }
        public Message message { get; set; }
        public object logprobs { get; set; }
        public string finish_reason { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
        public List<ToolCall> tool_calls { get; set; }
    }

    public class ToolCall
    {
        public string id { get; set; }
        public string type { get; set; }
        public Function function { get; set; }
    }

    public class Function
    {
        public string name { get; set; }
        public string arguments { get; set; }
        public AssistantResponse assistantResponse { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }


    public class AssistantResponse {
        public string assistant_response { get; set;}
        public bool user_wants_to_be_contacted { get; set;}
        public bool user_wants_to_install_the_demo { get; set;}
        public bool user_wants_to_schedule_call_with_sales_rep { get; set;}
        public string user_first_name { get; set;}
        public string user_last_name { get; set;}
        public string user_email { get; set;}
        public string user_phone_number { get; set;}
        public string user_company_name { get; set;}
        public string redirect_url { get; set;}
    }

    public static string EscapeStringForJson(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input.Replace("\n", " ")
                    .Replace("\r", " ")
                    .Replace("\t", " ")
                    .Replace("\b", " ")
                    .Replace("\f", " ")
                    .Replace("  ", " ")
                    .Replace("'", "")
                    ;
    }
    

}

