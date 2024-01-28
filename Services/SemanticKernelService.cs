using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Kernel = Microsoft.SemanticKernel.Kernel;
using System.IO;
using System.Diagnostics;
using System;
using Microsoft.AspNetCore.Hosting;
using SalesBotApi.Models;

public class SemanticKernelService
{
    private readonly ILoggerFactory myLoggerFactory = NullLoggerFactory.Instance;
    private readonly Kernel kernel;

    private readonly string promptTemplate = @"
You are a friendly and professional inbound sales representative for the company named ""{company_name}""
Here is the company description: ""{company_desc}""

You should be helpful and always very respectful and respond in a professional manner.
You should ALWAYS ONLY answer any questions that are relevant to this company.
You should NEVER engage in any conversation that is NOT relevant to this company.
If the user tries to ask questions unrelated to this company you should politely decline to answer
that question and offer to help answer questions about this company instead.

If you add any hyperlinks to your response they should always be in markdown format like this: [Display Text](https://example.com)

+++++
Here is some context relevant: {context_docs}
+++++
User Question: ""{user_question}""
+++++
    ";

    public SemanticKernelService(IWebHostEnvironment env)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(LogLevel.Trace));
        builder.Services
            .AddSingleton(myLoggerFactory)
            .AddAzureOpenAIChatCompletion(
                "keli-35-turbo",
                "https://keli-chatbot.openai.azure.com/",
                "6b22e2a31df942ed92e0e283614882aa"
            )
            ;
        kernel = builder.Build();
    }

    public async Task<string> SubmitUserQuestion(
        string userQuestion, 
        string[] contextDocs,
        Company company,
        Conversation convo,
        Chatbot chatbot
    )
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        // var arguments = new KernelArguments() { 
        //     ["user_question"] = userQuestion,
        //     ["context_docs"] = contextDocs
        // };
        string prompt = promptTemplate;
        prompt = replaceInPrompt(prompt, "company_name", company.name);
        prompt = replaceInPrompt(prompt, "company_desc", company.description);
        prompt = replaceInPrompt(prompt, "context_docs", string.Join("',\n'", contextDocs));
        prompt = replaceInPrompt(prompt, "user_question", userQuestion);
        Console.WriteLine(prompt);

        var result = await kernel.InvokePromptAsync(prompt);

        stopwatch.Stop();
        Console.WriteLine($"SubmitUserQuestion: {stopwatch.ElapsedMilliseconds} ms");
        return result.ToString();
    }

    private string replaceInPrompt(string prompt, string key, string value) {
        return prompt.Replace("{"+key+"}", value);
    }
}
