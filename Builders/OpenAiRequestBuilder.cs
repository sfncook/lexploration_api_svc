public class OpenAiRequestBuilder
{

    private string system_prompt;
    private string user_question;
    private string model;

    public OpenAiRequestBuilder setSystemPrompt(string system_prompt){
        this.system_prompt = system_prompt;
        return this;
    }
    public OpenAiRequestBuilder setUserQuestion(string user_question){
        this.user_question = user_question;
        return this;
    }
    public OpenAiRequestBuilder setModel(string model){
        this.model = model;
        return this;
    }

    private readonly string reqParamsTemplate = @"
{
  ""model"": ""{model}"",
  ""messages"": [
    {
      ""role"": ""system"",
      ""content"": ""{system_prompt}""
    },
    {
      ""role"": ""user"",
      ""content"": ""{user_question}""
    }
  ],
  ""temperature"": 0.7,
  ""tools"": [
    {
      ""type"": ""function"",
      ""function"": {
        ""name"": ""response_with_optional_user_data"",
        ""description"": ""The assistant response (text) along with optional user data and redirect URLs."",
        ""parameters"": {
          ""type"": ""object"",
          ""properties"": {
            ""assistant_response"": {
                ""type"": ""string"",
                ""description"": ""The assistant's (your) textual response to the user's last message.""
            },
            ""user_wants_to_be_contacted"": {
                ""type"": ""boolean"",
                ""description"": ""The user has expressed that they wish to be contacted by a human representative""
            },
            ""user_wants_to_install_the_demo"": {
                ""type"": ""boolean"",
                ""description"": ""The user has expressed that they wish to install the demo app""
            },
            ""user_wants_to_schedule_call_with_sales_rep"": {
                ""type"": ""boolean"",
                ""description"": ""The user has expressed that they wish to schedule a call with a sales rep""
            },
            ""user_first_name"": {
                ""type"": ""string"",
                ""description"": ""The user has told you their first name or 'given' name""
            },
            ""user_last_name"": {
                ""type"": ""string"",
                ""description"": ""The user has told you their last name or 'family' name""
            },
            ""user_email"": {
                ""type"": ""string"",
                ""description"": ""The user has told you their email address""
            },
            ""user_phone_number"": {
                ""type"": ""string"",
                ""description"": ""The user has told you their phone number""
            },
            ""user_company_name"": {
                ""type"": ""string"",
                ""description"": ""The user has told you the name of their company or organization they work for""
            },
            ""redirect_url"": {
                ""type"": ""string"",
                ""description"": ""The user has said satisfied one of the 'redirect prompts' and this is the URL we should redirect them to""
            }
          },
          ""required"": [
            ""assistant_response""
          ]
        }
      }
    }
  ],
  ""tool_choice"": {
    ""type"": ""function"",
    ""function"": {
      ""name"": ""response_with_optional_user_data""
    }
  }
}
";

    public string build() {
        string reqParams = reqParamsTemplate;
        reqParams = replaceInTemplate(reqParams, "system_prompt", EscapeStringForJson(system_prompt));
        reqParams = replaceInTemplate(reqParams, "user_question", EscapeStringForJson(user_question));
        reqParams = replaceInTemplate(reqParams, "model", model);
        return reqParams;
    }

    private string replaceInTemplate(string prompt, string key, string value) {
        return prompt.Replace("{"+key+"}", value);
    }

    public static string EscapeStringForJson(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input.Replace("\"", "\\\"")
                    .Replace("\n", " ")
                    .Replace("\r", " ")
                    .Replace("\t", " ")
                    .Replace("\b", " ")
                    .Replace("\f", " ");
    }
}