using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Extensions.Configuration;
using System.Text;

public class EmailService
{
    private readonly IHttpClientFactory clientFactory;

    public EmailService(IConfiguration configuration, IHttpClientFactory _clientFactory)
    {
        clientFactory = _clientFactory;
    }

    public async Task SendEmail(string _recipient_email, string _subject, string _body)
        {
            EmailRequest emailReq = new EmailRequest
            {
                recipient_email = _recipient_email,
                subject = _subject,
                body = _body
            };
            HttpClient client = clientFactory.CreateClient();
            string postUrl = "https://salesbot-001.azurewebsites.net/api/email";
            //            string postUrl = "http://localhost:7071/api/email";
            string body = JsonConvert.SerializeObject(emailReq);
            var postResponse = await client.PostAsync(postUrl, new StringContent(body, Encoding.UTF8, "application/json"));
        }
}
