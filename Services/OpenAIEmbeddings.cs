using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class OpenAIEmbeddings : IEmbeddingsProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAIEmbeddings()
    {
        _apiKey = "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";
        _httpClient = new HttpClient();
    }


    // curl https://api.openai.com/v1/embeddings \
    // -H "Content-Type: application/json" \
    // -H "Authorization: Bearer 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" \
    // -d '{
    //     "input": "Your text string goes here",
    //     "model": "text-embedding-3-small"
    // }'
    public async Task<float[]> GetEmbeddingsAsync(string text)
    {
        var requestBody = new
        {
            input = text,
            model = "text-embedding-3-small"
        };

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        // Replace HttpMethod.Get with HttpMethod.Post
        using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://api.openai.com/v1/embeddings"))
        {
            requestMessage.Content = content;
            requestMessage.Headers.Add("Authorization", $"Bearer {_apiKey}");

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
