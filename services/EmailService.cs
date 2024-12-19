using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public class EmailService
{
    private readonly string _gmailFrom;
    private readonly string _gmailUsername;
    private readonly string _gmailAppPassword;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _gmailUsername = configuration["Gmail:Username"];
        _gmailFrom = configuration["Gmail:From"];
        _gmailAppPassword = configuration["Gmail:AppPassword"];
        _logger = logger;

        // Log the values for clarity (this should be done cautiously in production environments)
        _logger.LogInformation("Loaded Gmail Username and AppPassword from config.");
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("CaseRelay", _gmailFrom));
        message.To.Add(new MailboxAddress("Recipient", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = body };
        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient
            {
                Timeout = 10000  // Set custom timeout for the operation (in milliseconds)
            };

            // Enable detailed logging
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            _logger.LogInformation("Connecting to SMTP server...");

            await client.ConnectAsync("smtp.gmail.com", 465, false);  // false = Use plain SMTP, use true for TLS
            _logger.LogInformation("Authenticated successfully.");

            await client.AuthenticateAsync(_gmailFrom, _gmailAppPassword);
            _logger.LogInformation("Sending email...");

            await client.SendAsync(message);
            _logger.LogInformation("Email sent successfully!");

            await client.DisconnectAsync(true);  // Disconnect after sending email
            _logger.LogInformation("Disconnected from SMTP server successfully.");
        }
        catch (TimeoutException ex)
        {
            _logger.LogError($"Timeout Error: {ex.Message}");
            throw new InvalidOperationException("SMTP operation timed out.", ex);
        }
        catch (SmtpCommandException ex)
        {
            _logger.LogError($"SMTP Command Error: {ex.Message}");
            throw new InvalidOperationException("Failed to send email due to SMTP command issue.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error sending email: {ex.Message}");
            throw new InvalidOperationException("An unexpected error occurred while sending email.", ex);
        }
    }
}
