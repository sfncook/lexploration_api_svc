using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Kernel = Microsoft.SemanticKernel.Kernel;
using System.IO;

// Memory functionality is experimental
#pragma warning disable SKEXP0003, SKEXP0011, SKEXP0052

public class SemanticKernelService
{
    private readonly ILoggerFactory myLoggerFactory = NullLoggerFactory.Instance;
    private readonly Kernel kernel;
    private readonly KernelPlugin funPluginFunctions;

    public SemanticKernelService()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services
            .AddSingleton(myLoggerFactory)
            .AddAzureOpenAIChatCompletion(
                "keli-35-turbo",
                "https://keli-chatbot.openai.azure.com/",
                "6b22e2a31df942ed92e0e283614882aa"
            )
            ;
        var funPluginDirectoryPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Plugins", "FunPlugin");

        kernel = builder.Build();

        funPluginFunctions = kernel.ImportPluginFromPromptDirectory(funPluginDirectoryPath);
    }

    public async Task<string> GetJoke()
    {
        var arguments = new KernelArguments() { ["input"] = "time travel to dinosaur age" };
        var result = await kernel.InvokeAsync(funPluginFunctions["Joke"], arguments);
        return result.ToString();
    }
}
