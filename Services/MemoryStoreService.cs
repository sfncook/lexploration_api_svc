using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Pinecone;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Linq;
using System.Text;

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

        // curl -X POST "https://companies-x9v8jnv.svc.gcp-starter.pinecone.io/vectors/upsert" \
        // -H "Api-Key: fcafedc4-cf32-4b4a-9d26-08fc227cf526" \
        // -H 'Content-Type: application/json' \
        // -d '{
        //     "vectors": [
        //     {
        //         "id": "vec1", 
        //         "values": [],
        //         "metadata": {
        //             "salesbot": "We sell brown horses, but no other colors of horses.",
        //             "source": "https://example.com"
        //         }
        //     },
        //     ],
        //     "namespace": "ns1"
        // }'
    public async Task Write(string documentStr, string source) {
        AzureOpenAIEmbeddings openAIEmbeddings = new AzureOpenAIEmbeddings();
        float[] vectorFltAr = await openAIEmbeddings.GetEmbeddingsAsync(documentStr);
        // string vectorStr = string.Join(", ", vectorFltAr);

        string id = "vec2";
        Metadata metadata = new Metadata
        {
            salesbot = documentStr,
            source = source
        };

        Vector vector = new Vector
        {
            id = id,
            values = vectorFltAr,
            metadata = metadata
        };

        Vector[] vectors = new Vector[1];
        vectors[0] = vector; // Assigning vector to the first element of the array

        WriteRequest writeRequest = new WriteRequest
        {
            vectors = vectors,
            @namespace = "ns1"
        };

        // {"vectors":[{"id":"vec2","values":[],"metadata":{"salesbot":"My name is Shawn","source":"https://example.com"}}]}
        string body = JsonConvert.SerializeObject(writeRequest);
        Console.WriteLine(body);
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://companies-x9v8jnv.svc.gcp-starter.pinecone.io/vectors/upsert"))
        {
            requestMessage.Content = content;
            requestMessage.Headers.Add("api-key", apiKey);

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
            // var embeddingsResponse = JsonConvert.DeserializeObject<PineconeQueryResponse>(responseString);
            // return embeddingsResponse;
        }
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
        string body = @"{""namespace"": ""ns1"",""vector"": [XXX],""topK"": 2,""includeValues"": false,""includeMetadata"": true}";
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


    // -d '{
    //     "vectors": [
    //     {
    //         "id": "vec1", 
    //         "values": [],
    //         "metadata": {
    //             "salesbot": "We sell brown horses, but no other colors of horses.",
    //             "source": "https://example.com"
    //         }
    //     },
    //     ],
    //     "namespace": "ns1"
    // }'

    public class WriteRequest {
        public Vector[] vectors { get; set; }
        public string @namespace { get; set; }
    }
    
    public class Vector {
        public string id { get; set; }
        public float[] values { get; set; }
        public Metadata metadata { get; set; }
    }

}
