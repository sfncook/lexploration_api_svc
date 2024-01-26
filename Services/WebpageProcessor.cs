using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;

public class WebpageProcessor
{
    private HttpClient _httpClient;

    public WebpageProcessor()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "SalesChatbot/1.0");
    }

    public async Task<string[]> GetTextChunksFromUrlAsync(string url, int chunkSize)
    {
        // Download webpage content
        Console.WriteLine(url);
        string htmlContent = await _httpClient.GetStringAsync(url);

        // Extract text from HTML
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        // Get only the visible text
        var textNodes = doc.DocumentNode
            .SelectNodes("//text()[normalize-space(.) != '']")
            .Where(node => node.ParentNode.Name != "script" && node.ParentNode.Name != "style");
        var textContent = string.Join(" ", textNodes.Select(node => node.InnerText));

        // Chunk the text
        return ChunkText(textContent, chunkSize);

        // var client = new HttpClient();
        // var request = new HttpRequestMessage(HttpMethod.Get, "https://www.blacktiecasinoevents.com");
        // client.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.36.1");
        // var response = await client.SendAsync(request);
        // response.EnsureSuccessStatusCode();
        // Console.WriteLine(await response.Content.ReadAsStringAsync());
        // return new string[0];
    }

    private string[] ChunkText(string text, int chunkSize)
    {
        var chunks = new List<string>();
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
        }
        return chunks.ToArray();
    }
}
