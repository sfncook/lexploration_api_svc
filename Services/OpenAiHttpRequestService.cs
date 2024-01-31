using System.Threading.Tasks;
using SalesBotApi.Models;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System;

public class OpenAiHttpRequestService
{

    private readonly HttpClient _httpClient;
    public OpenAiHttpRequestService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<AssistantResponse> SubmitUserQuestion(
        string userQuestion, 
        string[] contextDocs,
        Company company,
        Conversation convo,
        Chatbot chatbot,
        IEnumerable<Refinement> refinements,
        IEnumerable<Message> messages
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

        Console.WriteLine(prompt);

        OpenAiRequestBuilder openAiRequestBuilder = new OpenAiRequestBuilder();
        string reqParams = openAiRequestBuilder
            .setModel(chatbot.llm_model)
            .setUserQuestion(userQuestion)
            .setSystemPrompt(prompt)
            .setMessages(messages)
            .build();

        reqParams = EscapeStringForJson(reqParams);

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
            AssistantResponse assistantResponse;
            // Sometime we're getting JSON failure parsion the function arguments, so I'm assuming the LLM doesn't always call the function and sometimes it
            //  screws up and just returns a string (message.content).  Hence this try-catch block.
            try {
                assistantResponse = JsonConvert.DeserializeObject<AssistantResponse>(argumentsStr);
            } catch(JsonReaderException) {
                // TODO add count metrics for how ofte this happens
                string messageContent = chatCompletionResponse.choices[0].message.content;
                if(messageContent!=null){
                    Console.WriteLine($"JSON Exception trying to parse assistant response argumentsStr:{argumentsStr} but message.content was NON NULL:{messageContent}");
                    assistantResponse = new AssistantResponse {
                        assistant_response = messageContent
                    };
                } else {
                    Console.WriteLine($"JSON Exception trying to parse assistant response argumentsStr:{argumentsStr} and message.content was NULL so throwing the exception :(");
                    throw;
                }
            }
            return assistantResponse;
        }
    }

    public class FunctionDef {
        public string name { get; set;}
        public string description { get; set;}
        public AssistantResponse parameters { get; set;}
    }

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
        public _Message message { get; set; }
        public object logprobs { get; set; }
        public string finish_reason { get; set; }
    }

    public class _Message
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

