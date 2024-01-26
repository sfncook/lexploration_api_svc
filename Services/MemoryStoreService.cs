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
    private readonly IMemoryStore asdf;
    private readonly HttpClient _httpClient;

    public MemoryStoreService()
    {
        string pineconeEnvironment = "gcp-starter";
        string apiKey = "fcafedc4-cf32-4b4a-9d26-08fc227cf526";
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

    public async Task Read(string question) {
        AzureOpenAIEmbeddings openAIEmbeddings = new AzureOpenAIEmbeddings();
        float[] embeddings = await openAIEmbeddings.GetEmbeddingsAsync(question);
        string embeddingsStr = string.Join(", ", embeddings);

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

        // var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
        string body = @"{""namespace"": ""saleschat_bot"",""vector"": [XXX],""topK"": 2,""includeValues"": true,""includeMetadata"": true,""filter"": {""genre"": {""$eq"": ""action""}}}";
        body = body.Replace("XXX", embeddingsStr);
        Console.WriteLine(body);
        var content = new StringContent(body);

        // Replace HttpMethod.Get with HttpMethod.Post
        using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://companies-x9v8jnv.svc.gcp-starter.pinecone.io/query"))
        {
            requestMessage.Content = content;
            requestMessage.Headers.Add("api-key", "fcafedc4-cf32-4b4a-9d26-08fc227cf526");

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
            // var embeddingsResponse = JsonConvert.DeserializeObject<EmbeddingsResponse>(responseString);

            // return embeddingsResponse.data.FirstOrDefault()?.embedding;
        }

        // string indexName = "companies";
        // string indexNamespace = "blacktiecasinoevents";
        // int limit = 5;
        // IAsyncEnumerable<(MemoryRecord, double)> respPinecone = pineconeMemory.GetNearestMatchesFromNamespaceAsync(
        //     indexName, 
        //     indexNamespace, 
        //     resp, // ReadOnlyMemory<float> embedding, 
        //     limit 
        //     // double minRelevanceScore = 0.0, 
        //     // bool withEmbeddings = false, 
        //     // [EnumeratorCancellation] CancellationToken cancellationToken = default(CancellationToken)
        // );
        // Console.WriteLine(respPinecone.ToString());

        // const string memoryCollectionName = "SKGitHub";
        // string ask = "I love Jupyter notebooks, how should I get started?";
        // Console.WriteLine("===========================\n" +
        //                     "Query: " + ask + "\n");

        // var memories = memory.SearchAsync(memoryCollectionName, ask, limit: 5, minRelevanceScore: 0.77);

        // var i = 0;
        // await foreach (var memory in memories)
        // {
        //     Console.WriteLine($"Result {++i}:");
        //     Console.WriteLine("  URL:     : " + memory.Metadata.Id);
        //     Console.WriteLine("  Title    : " + memory.Metadata.Description);
        //     Console.WriteLine("  Relevance: " + memory.Relevance);
        //     Console.WriteLine();
        // }
    }

    // private class PcReqBody
    // {
    //     // "namespace": "saleschat_bot",
    //     // "vector": [0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3],
    //     // "topK": 2,
    //     // "": true,
    //     // "": true,
    //     // "filter": {"genre": {"$eq": "action"}}
    //     public string Namespace { get; set; }
    //     public float[] vector { get; set; }
    //     public int topK { get; set; }
    //     public bool includeValues { get; set; }
    //     public bool includeMetadata { get; set; }
    // }

}
