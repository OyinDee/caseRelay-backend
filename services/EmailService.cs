using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using CaseRelayAPI.Models;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Log the values for clarity (this should be done cautiously in production environments)
        _logger.LogInformation("Loaded email settings from config.");
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var emailSettings = new
        {
            SmtpServer = _configuration["EMAIL_SMTP_SERVER"],
            Port = int.Parse(_configuration["EMAIL_SMTP_PORT"]),
            SenderEmail = _configuration["EMAIL_SENDER"],
            SenderPassword = _configuration["EMAIL_PASSWORD"],
            SenderName = _configuration["EMAIL_FROM_NAME"],
            UseSsl = true
        };

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(emailSettings.SenderName, emailSettings.SenderEmail));
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

            await client.ConnectAsync(emailSettings.SmtpServer, emailSettings.Port, emailSettings.UseSsl);
            _logger.LogInformation("Connected to SMTP server.");

            await client.AuthenticateAsync(emailSettings.SenderEmail, emailSettings.SenderPassword);
            _logger.LogInformation("Authenticated successfully.");

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

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        var subject = "Password Reset Request";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; color: #000000;'>
                <h2>Password Reset Request</h2>
                <p>Dear User,</p>
                <p>You have requested to reset your password. Click the link below to reset your password. This link will expire in 1 hour:</p>
                <p><a href='{resetLink}'>Reset Password</a></p>
                <p>If you did not request a password reset, please ignore this email or contact support if you have concerns.</p>
                <p>Best regards,<br>CaseRelay Team</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }
}
