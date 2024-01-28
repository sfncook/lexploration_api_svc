using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Kernel = Microsoft.SemanticKernel.Kernel;
using System.Diagnostics;
using System;
using Microsoft.AspNetCore.Hosting;
using SalesBotApi.Models;
using System.Collections.Generic;

public class SemanticKernelService
{
    private readonly ILoggerFactory myLoggerFactory = NullLoggerFactory.Instance;
    private readonly Kernel kernel;

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
        Chatbot chatbot,
        IEnumerable<Refinement> refinements
    )
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        PromptBuilder promptBuilder = new PromptBuilder();
        promptBuilder.setUserQuestion(userQuestion);
        promptBuilder.setCompany(company);
        promptBuilder.setChatbot(chatbot);
        promptBuilder.setContextDocs(contextDocs);
        promptBuilder.setConversation(convo);
        string prompt = promptBuilder.build();
        Console.WriteLine(prompt);

        var result = await kernel.InvokePromptAsync(prompt);

        stopwatch.Stop();
        Console.WriteLine($"SubmitUserQuestion: {stopwatch.ElapsedMilliseconds} ms");
        return result.ToString();
    }

}
