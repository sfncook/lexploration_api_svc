using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Kernel = Microsoft.SemanticKernel.Kernel;
using System;
using Microsoft.AspNetCore.Hosting;
using SalesBotApi.Models;
using System.Collections.Generic;
using Plugins;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;

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
        builder.Plugins.AddFromType<FormatAssistantResponsePlugin>();
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
        // Enable auto function calling
        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions
        };

        // Get the response from the AI
        ChatHistory history = new ChatHistory();
        PromptBuilder promptBuilder = new PromptBuilder();
        promptBuilder.setUserQuestion(userQuestion);
        promptBuilder.setCompany(company);
        promptBuilder.setChatbot(chatbot);
        promptBuilder.setContextDocs(contextDocs);
        promptBuilder.setConversation(convo);
        string prompt = promptBuilder.build();
        Console.WriteLine(prompt);
        history.AddSystemMessage(prompt);
        history.AddUserMessage("Hello my name is shawn");
        IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var result = chatCompletionService.GetStreamingChatMessageContentsAsync(
            history,
            executionSettings: openAIPromptExecutionSettings,
            kernel: kernel);

        // Stream the results
        string fullMessage = "";
        var first = true;
        await foreach (var content in result)
        {
            if (content.Role.HasValue && first)
            {
                Console.Write("Assistant > ");
                first = false;
            }
            Console.Write(content.Content);
            fullMessage += content.Content;
        }
        return fullMessage;

        // Stopwatch stopwatch = Stopwatch.StartNew();
        // PromptBuilder promptBuilder = new PromptBuilder();
        // promptBuilder.setUserQuestion(userQuestion);
        // promptBuilder.setCompany(company);
        // promptBuilder.setChatbot(chatbot);
        // promptBuilder.setContextDocs(contextDocs);
        // promptBuilder.setConversation(convo);
        // string prompt = promptBuilder.build();
        // Console.WriteLine(prompt);

        // var result = await kernel.InvokePromptAsync(prompt);

        // stopwatch.Stop();
        // Console.WriteLine($"SubmitUserQuestion: {stopwatch.ElapsedMilliseconds} ms");
        // return result.ToString();
    }

}
