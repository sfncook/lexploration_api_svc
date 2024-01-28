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

    public async Task<string> SubmitUserQuestion(string userQuestion, string[] contextDocs)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        // var arguments = new KernelArguments() { 
        //     ["user_question"] = userQuestion,
        //     ["context_docs"] = contextDocs
        // };
        // var result = await kernel.InvokeAsync(salesBotPluginFunctions["UserQuestion"], arguments);
        string prompt = $"Say the words 'foo bar' to me.";
        var result = await kernel.InvokePromptAsync(prompt);
        stopwatch.Stop();
        Console.WriteLine($"SubmitUserQuestion: {stopwatch.ElapsedMilliseconds} ms");
        return result.ToString();
    }
}
