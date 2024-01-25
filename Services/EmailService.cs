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

    public async Task SendRegistrationEmail(string recipient_email) {
            await SendEmail(
                "hello@saleschat.bot", 
                "SalesChatbot", 
                recipient_email, 
                "SalesChat.bot registration received", 
                @"
Hello! 

Thanks for registering for a free trial account at SalesChat.bot. We will approve your account as quickly as possible (usually within 24 hours) and let you know. If you haven't heard from us within a few days, feel free to reply to this email. 

Thank you,
The SalesChat.bot team
                "
            );
        }

    public async Task SendRegistrationApprovalEmail(string recipient_email) {
            await SendEmail(
                "hello@saleschat.bot", 
                "SalesChatbot", 
                recipient_email, 
                "Your SalesChat.bot registration was approved", 
                @"
Hello! 

Good news -- your SalesChat.bot registration was approved, and your account is now active. Please log in here: https://admin.saleschat.bot

See our Getting Started guide at https://docs.saleschat.bot

If you have any questions, feel free to reply to this email.

Thank you,
The SalesChat.bot team
                "
            );
        }

        public async Task SendRegistrationDeniedEmail(string recipient_email) {
            await SendEmail(
                "hello@saleschat.bot", 
                "SalesChatbot", 
                recipient_email, 
                "Your SalesChat.bot registration was declined", 
                @"
Hello! 

Unfortunately, your SalesChat.bot registration was declined. Be sure to use a company email address when registering for an account. We don't accept personal emails, e.g. Gmail, Yahoo, Hotmail, etc. See here for more information: https://docs.saleschat.bot

If you have any questions, feel free to reply to this email.

Thank you,
The SalesChat.bot team
                "
            );
        }

}
