using System;
using System.ComponentModel;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;

namespace Plugins;

public class FormatAssistantResponsePlugin
{
    [KernelFunction, Description("Format the assistant's response into a well-formatted JSON string.")]
    public static string AssistantResponseToJson(
        [Description("The assistant's (your) textual response to the user's last message")] string _assistant_response,
        [Description("The user has expressed that they wish to be contacted by a human representative")] bool _user_wants_to_be_contacted,
        [Description("The user has expressed that they wish to install the demo app")] bool _user_wants_to_install_the_demo,
        [Description("The user has expressed that they wish to schedule a call with a sales rep")] bool _user_wants_to_schedule_call_with_sales_rep,
        [Description("The user has told you their first name or 'given' name")] string _user_first_name,
        [Description("The user has told you their last name or 'family' name")] string _user_last_name,
        [Description("The user has told you their email address")] string _user_email,
        [Description("The user has told you their phone number")] string _user_phone_number,
        [Description("The user has told you the name of their company or organization they work for")] string _user_company_name,
        [Description("The user has said satisfied one of the 'redirect prompts' and this is the URL we should redirect them to")] string _redirect_url
    )
    {
        Console.WriteLine("FormatAssistantResponse");
        AssistantResponse2 resp = new AssistantResponse2
        {
            assistant_response = _assistant_response,
            user_wants_to_be_contacted = _user_wants_to_be_contacted,
            user_wants_to_install_the_demo = _user_wants_to_install_the_demo,
            user_wants_to_schedule_call_with_sales_rep = _user_wants_to_schedule_call_with_sales_rep,
            user_first_name = _user_first_name,
            user_last_name = _user_last_name,
            user_email = _user_email,
            user_phone_number = _user_phone_number,
            user_company_name = _user_company_name,
            redirect_url = _redirect_url
        };
        return JsonConvert.SerializeObject(resp);
    }
}

public class AssistantResponse2 {
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