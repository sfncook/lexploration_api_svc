using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Pinecone;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;

// Memory functionality is experimental
#pragma warning disable SKEXP0003 // ISemanticTextMemory
#pragma warning disable SKEXP0011 // WithAzureOpenAITextEmbeddingGeneration
#pragma warning disable SKEXP0031 // Pinecone

public class MemoryStoreService
{
    private readonly ISemanticTextMemory memory;
    private readonly PineconeMemoryStore pineconeMemory;
    private readonly HttpClient _httpClient;
    string pineconeEnvironment = "gcp-starter";
    string apiKey = "fcafedc4-cf32-4b4a-9d26-08fc227cf526";

    public MemoryStoreService()
    {
        PineconeClient pineconeClient = new PineconeClient(pineconeEnvironment, apiKey);
        pineconeMemory = new PineconeMemoryStore(pineconeClient);

        var memoryBuilder = new MemoryBuilder();
        memoryBuilder.WithAzureOpenAITextEmbeddingGeneration(
            "salesbot-text-embedding-ada-002",
            "https://keli-chatbot.openai.azure.com/", 
            "6b22e2a31df942ed92e0e283614882aa"
        );
        memoryBuilder.WithMemoryStore(pineconeMemory);
        memory = memoryBuilder.Build();

        _httpClient = new HttpClient();
    }

    public async Task Write() {
        const string MemoryCollectionName = "aboutMe";
        await memory.SaveInformationAsync(MemoryCollectionName, id: "info1", text: "My name is Andrea");

        // const string memoryCollectionName = "SKGitHub";
        // var githubFiles = new Dictionary<string, string>()
        // {
        //     ["https://github.com/microsoft/semantic-kernel/blob/main/README.md"]
        //         = "README: Installation, getting started, and how to contribute",
        //     ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/02-running-prompts-from-file.ipynb"]
        //         = "Jupyter notebook describing how to pass prompts from a file to a semantic plugin or function",
        //     ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/00-getting-started.ipynb"]
        //         = "Jupyter notebook describing how to get started with the Semantic Kernel",
        //     ["https://github.com/microsoft/semantic-kernel/tree/main/samples/plugins/ChatPlugin/ChatGPT"]
        //         = "Sample demonstrating how to create a chat plugin interfacing with ChatGPT",
        //     ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/Plugins/Plugins.Memory/VolatileMemoryStore.cs"]
        //         = "C# class that defines a volatile embedding store",
        // };
        // MemoryRecord mr;
        // var i = 0;
        // foreach (var entry in githubFiles)
        // {
        //     await memory.SaveReferenceAsync(
        //         collection: memoryCollectionName,
        //         description: entry.Value,
        //         text: entry.Value,
        //         externalId: entry.Key,
        //         externalSourceName: "GitHub"
        //     );
        //     Console.WriteLine($"  URL {++i} saved");
        // }
    }

        // curl -X POST "https://companies-x9v8jnv.svc.gcp-starter.pinecone.io/query" \
        // -H "Api-Key: fcafedc4-cf32-4b4a-9d26-08fc227cf526" \
        // -H 'Content-Type: application/json' \
        // -d '{
        //     "namespace": "saleschat_bot",
        //     "vector": [0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3],
        //     "topK": 2,
        //     "includeValues": true,
        //     "includeMetadata": true,
        //     "filter": {"genre": {"$eq": "action"}}
        // }'
    public async Task<PineconeQueryResponse> Read(string question)
    {
        AzureOpenAIEmbeddings openAIEmbeddings = new AzureOpenAIEmbeddings();
        float[] embeddings = await openAIEmbeddings.GetEmbeddingsAsync(question);
        string embeddingsStr = string.Join(", ", embeddings);

        // var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
        string body = @"{""namespace"": ""saleschat_bot"",""vector"": [XXX],""topK"": 2,""includeValues"": false,""includeMetadata"": true}";
        body = body.Replace("XXX", embeddingsStr);
        // Console.WriteLine(body);
        var content = new StringContent(body);

        // Replace HttpMethod.Get with HttpMethod.Post
        using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://companies-x9v8jnv.svc.gcp-starter.pinecone.io/query"))
        {
            requestMessage.Content = content;
            requestMessage.Headers.Add("api-key", apiKey);

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            // Console.WriteLine(responseString);
            var embeddingsResponse = JsonConvert.DeserializeObject<PineconeQueryResponse>(responseString);
            return embeddingsResponse;
        }
    }

    // {
    // "results": [],
    // "matches": [
    //     {
    //         "id": "537fd751-3948-4c79-abd3-6e6bc9907765",
    //         "score": 0.83706373,
    //         "values": [],
    //         "metadata": {
    //             "salesbot": "Get started today and transform your website with our AI-powered Sales\nChatbot.\n\nGet Started\n\nCopyright © 2024 Sales Chatbot",
    //             "source": "https://saleschat.bot/"
    //         }
    //     },
    //     {
    //         "id": "f24ea129-7e37-46b5-aa51-e77460175045",
    //         "score": 0.836923659,
    //         "values": [],
    //         "metadata": {
    //             "salesbot": "Get started today and transform your website with our AI-powered Sales\nChatbot.\n\nGet Started\n\nCopyright © 2024 Sales Chatbot",
    //             "source": "https://saleschat.bot/#content"
    //         }
    //     }
    // ],
    // "namespace": "saleschat_bot",
    // "usage": {
    //     "readUnits": 6
    // }
    // }

    public class PineconeQueryResponse
    {
        public Match[] matches { get; set; }
    }

    public class Match
    {
        public string id { get; set; }
        public float score { get; set; }
        public Metadata metadata { get; set; }
    }

    public class Metadata
    {
        public string salesbot { get; set; }
        public string source { get; set; }
    }

}
