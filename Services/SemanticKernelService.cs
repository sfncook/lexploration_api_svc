using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Kernel = Microsoft.SemanticKernel.Kernel;
using System.IO;
using System.Diagnostics;
using System;

public class SemanticKernelService
{
    private readonly ILoggerFactory myLoggerFactory = NullLoggerFactory.Instance;
    private readonly Kernel kernel;
    private readonly KernelPlugin salesBotPluginFunctions;

    public SemanticKernelService()
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
        var userQuestionPluginDirectoryPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Plugins", "SalesBot");
        kernel = builder.Build();
        salesBotPluginFunctions = kernel.ImportPluginFromPromptDirectory(userQuestionPluginDirectoryPath);
    }

    public async Task<string> SubmitUserQuestion(string userQuestion, string[] contextDocs)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        var arguments = new KernelArguments() { 
            ["user_question"] = userQuestion,
            ["context_docs"] = contextDocs
        };
        var result = await kernel.InvokeAsync(salesBotPluginFunctions["UserQuestion"], arguments);
        stopwatch.Stop();
        Console.WriteLine($"SubmitUserQuestion: {stopwatch.ElapsedMilliseconds} ms");
        return result.ToString();
    }
}
