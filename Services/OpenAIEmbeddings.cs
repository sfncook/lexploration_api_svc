using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

public class OpenAIEmbeddings : IEmbeddingsProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAIEmbeddings(IOptions<MyConnectionStrings> _myConnectionStrings)
    {
        _apiKey = _myConnectionStrings.Value.OpenAiApiKey;
        _httpClient = new HttpClient();
    }

    public async Task<float[]> GetEmbeddingsAsync(string text)
    {
        var requestBody = new
        {
            input = text,
            model = "text-embedding-3-small"
        };

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

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

    private class EmbeddingsResponse
    {
        public EmbeddingsData[] data { get; set; }
    }

    private class EmbeddingsData
    {
        public float[] embedding { get; set; }
    }
}
