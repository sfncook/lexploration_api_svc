using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class AzureOpenAIEmbeddings
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly string deployment = "salesbot-text-embedding-ada-002";

    public AzureOpenAIEmbeddings()
    {
        _apiKey = "6b22e2a31df942ed92e0e283614882aa";
        _endpoint = "https://keli-chatbot.openai.azure.com/";
        _httpClient = new HttpClient();
        // _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }


// curl https://keli-chatbot.openai.azure.com/openai/deployments/salesbot-text-embedding-ada-002/embeddings?api-version=2023-05-15 \
//   -H "Content-Type: application/json" \
//   -H "api-key: 6b22e2a31df942ed92e0e283614882aa" \
//   -d "{\"input\": \"The food was delicious and the waiter...\"}"

    public async Task<float[]> GetEmbeddingsAsync(string text)
    {
        var requestBody = new
        {
            input = text
        };

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        // Replace HttpMethod.Get with HttpMethod.Post
        using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/openai/deployments/{deployment}/embeddings?api-version=2023-05-15"))
        {
            requestMessage.Content = content;
            requestMessage.Headers.Add("api-key", _apiKey);

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var embeddingsResponse = JsonConvert.DeserializeObject<EmbeddingsResponse>(responseString);

            return embeddingsResponse.data.FirstOrDefault()?.embedding;
        }
    }


// {
//   "object": "list",
//   "data": [
//     {
//       "object": "embedding",
//       "index": 0,
//       "embedding": [
//          ...   
//         -0.015327491,
//         -0.019378418,
//         -0.0028842222
//       ]
//     }
//   ],
//   "model": "/var/azureml-app/azureml-models/text-embedding-ada-002-8k/584175/",
//   "usage": {
//     "prompt_tokens": 8,
//     "total_tokens": 8
//   }
// }

    private class EmbeddingsResponse
    {
        public EmbeddingsData[] data { get; set; }
    }

    private class EmbeddingsData
    {
        public float[] embedding { get; set; }
    }
}
