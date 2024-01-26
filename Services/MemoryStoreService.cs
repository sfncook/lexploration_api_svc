using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Diagnostics;

public class MemoryStoreService
{
    private readonly HttpClient _httpClient =  new HttpClient();
    private readonly AzureOpenAIEmbeddings openAIEmbeddings = new AzureOpenAIEmbeddings();
    string apiKey = "fcafedc4-cf32-4b4a-9d26-08fc227cf526";
    string pineconeHost = "https://companies-x9v8jnv.svc.gcp-starter.pinecone.io";


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

    public async Task<float[]> GetVector(string question)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        // TODO: Add latency metric
        float[] vectorFloatArr = await openAIEmbeddings.GetEmbeddingsAsync(question);
        stopwatch.Stop();
        Console.WriteLine($"GetVector: {stopwatch.ElapsedMilliseconds} ms");
        return vectorFloatArr;
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
