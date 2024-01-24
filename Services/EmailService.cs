using System.Net.Http;
using System.Threading.Tasks;
using SalesBotApi.Models;

public class EmailService
{
    private readonly IHttpClientFactory clientFactory;
    private readonly QueueService queueService;

    public EmailService(
        IHttpClientFactory _clientFactory,
        QueueService _queueService
    )
    {
        clientFactory = _clientFactory;
        queueService = _queueService;
    }

    public async Task SendEmail(string _sender_email, string _sender_name, string _recipient_email, string _subject, string _body)
    {
        EmailRequest emailReq = new EmailRequest
        {
            sender_email = _sender_email,
            sender_name = _sender_name,
            recipient_email = _recipient_email,
            subject = _subject,
            body = _body
        };

        await queueService.EnqueueSendEmailMessageAsync(emailReq);
    }

}
