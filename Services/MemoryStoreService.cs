using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Diagnostics;
using Microsoft.Extensions.Options;

public class MemoryStoreService
{
    private readonly HttpClient _httpClient =  new HttpClient();
    private readonly AzureOpenAIEmbeddings azureEmbeddings;
    private readonly OpenAIEmbeddings openaiEmbeddings;
    string apiKey;
    string pineconeHost;

    public MemoryStoreService(
        AzureOpenAIEmbeddings azureEmbeddings, 
        OpenAIEmbeddings openaiEmbeddings,
        IOptions<MySettings> _mySettings,
        IOptions<MyConnectionStrings> _myConnectionStrings
    )
    {
        pineconeHost = _mySettings.Value.PineConeHost;
        apiKey = _myConnectionStrings.Value.PineConeApiKey;
        this.azureEmbeddings = azureEmbeddings;
        this.openaiEmbeddings = openaiEmbeddings;
    }

    public async Task<string> Write(string documentStr, string url, string company_id) {
        float[] vectorFltAr = await openaiEmbeddings.GetEmbeddingsAsync(documentStr);
        
        string id = Guid.NewGuid().ToString();
        Metadata metadata = new Metadata
        {
            salesbot = documentStr,
            source = url
        };

        Vector vector = new Vector
        {
            id = id,
            values = vectorFltAr,
            metadata = metadata
        };

        Vector[] vectors = new Vector[1];
        vectors[0] = vector;

        WriteRequest writeRequest = new WriteRequest
        {
            vectors = vectors,
            @namespace = company_id
        };

        string body = JsonConvert.SerializeObject(writeRequest);
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{pineconeHost}/vectors/upsert"))
        {
            requestMessage.Content = content;
            requestMessage.Headers.Add("api-key", apiKey);

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            return id;
        }
    }

    public async Task<float[]> GetVectorOpenAi(string question)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        float[] vectorFloatArr =  await GetVector(openaiEmbeddings, question);
        stopwatch.Stop();
        Console.WriteLine($"--> METRICS (OpenaAi-Embeddings) GetVector: {stopwatch.ElapsedMilliseconds} ms");
        return vectorFloatArr; 
    }

    public async Task<float[]> GetVectorAzure(string question)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        float[] vectorFloatArr =  await GetVector(azureEmbeddings, question);
        stopwatch.Stop();
        Console.WriteLine($"--> METRICS (Azure-Embeddings) GetVector: {stopwatch.ElapsedMilliseconds} ms");
        return vectorFloatArr;
    }
    public async Task<float[]> GetVector(IEmbeddingsProvider embeddingsProvider, string question)
    {
        // TODO: Add latency metric
        return await embeddingsProvider.GetEmbeddingsAsync(question);
    }

    public async Task<string[]> GetRelevantContexts(float[] vectorFloatArr, string companyId)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        PineconeQueryRequest req = new PineconeQueryRequest
        {
            @namespace = companyId,
            vector = vectorFloatArr,
            topK = 2,
            includeValues = false,
            includeMetadata = true
        };

        string body = JsonConvert.SerializeObject(req);
        // Console.WriteLine(body);
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        // TODO: Add latency metric
        using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{pineconeHost}/query"))
        {
            requestMessage.Content = content;
            requestMessage.Headers.Add("api-key", apiKey);

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            // Console.WriteLine(responseString);
            var embeddingsResponse = JsonConvert.DeserializeObject<PineconeQueryResponse>(responseString);
            if (embeddingsResponse.matches == null) 
            {
                // Handle the scenario when matches is null
                return Array.Empty<string>();
            }

            var resp = embeddingsResponse.matches
                .Where(match => match != null && match.metadata != null && match.metadata.salesbot != null)
                .Select(match => match.metadata.salesbot)
                .ToArray();

            stopwatch.Stop();
            Console.WriteLine($"GetRelevantContexts: {stopwatch.ElapsedMilliseconds} ms");
            return resp;
        }
    }

    public class PineconeQueryRequest
    {
        public string @namespace { get; set; }
        public float[] vector { get; set; }
        public int topK { get; set; }
        public bool includeValues { get; set; }
        public bool includeMetadata { get; set; }
    }

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
