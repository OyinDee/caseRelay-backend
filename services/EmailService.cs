using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

public class EmailService
{
    private readonly string _gmailUsername;
    private readonly string _gmailAppPassword;

    public EmailService(IConfiguration configuration)
    {
        _gmailUsername = configuration["Gmail:Username"];
        _gmailAppPassword = configuration["Gmail:AppPassword"];
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_gmailUsername, _gmailUsername));
        message.To.Add(new MailboxAddress("Recipient Name", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = body };
        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, false);
            await client.AuthenticateAsync(_gmailUsername, _gmailAppPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to send email.", ex);
        }
    }
}
