using CaseRelayAPI.Models;

using CaseRelayAPI.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MailKit.Net.Smtp;
using MimeKit;

namespace CaseRelayAPI.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailSettings _emailSettings;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _emailSettings = configuration.GetSection("EmailSettings").Get<EmailSettings>();
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.SenderEmail));
            emailMessage.To.Add(new MailboxAddress("Recipient Name", toEmail));
            emailMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, false);
                await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.SenderPassword);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }

        public async Task<AuthResult> RegisterUserAsync(User user, string passcode)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.PoliceId == user.PoliceId);

            if (existingUser != null)
                return AuthResult.Failure("User already exists");

            user.PasscodeHash = BCrypt.Net.BCrypt.HashPassword(passcode);
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;
            user.IsVerified = false;
            user.FailedLoginAttempts = 0;
            user.RequirePasswordReset = false;

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; color: #000000;'>
                        <h2>Welcome to CaseRelay!</h2>
                        <p>Dear {user.FirstName},</p>
                        <p>Your account has been created successfully. You can now log in and start using CaseRelay.</p>
                        <p>Best regards,<br>CaseRelay Team</p>
                    </body>
                    </html>";

                await SendEmailAsync(user.Email, "Welcome to CaseRelay!", emailBody);

                return AuthResult.Success(user, null);
            }
            catch (Exception)
            {
                return AuthResult.Failure("Registration failed");
            }
        }

        public async Task<AuthResult> AuthenticateAsync(string policeId, string passcode)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PoliceId == policeId);

            if (user == null)
                return AuthResult.Failure("No Police Account Found!");

            if (!user.IsActive)
            {
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value <= DateTime.UtcNow)
                {
                    user.IsActive = true;
                    user.FailedLoginAttempts = 0;
                    user.LockoutEnd = null;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    return AuthResult.Failure("Account is locked. Try again later.");
                }
            }

            if (!BCrypt.Net.BCrypt.Verify(passcode, user.PasscodeHash))
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= 5)
                {
                    user.IsActive = false;
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                    await _context.SaveChangesAsync();

                    var emailBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; color: #000000;'>
                            <h2>Account Locked</h2>
                            <p>Dear {user.FirstName},</p>
                            <p>Your account has been locked due to multiple failed login attempts. Please try again after 30 minutes.</p>
                            <p>Best regards,<br>CaseRelay Team</p>
                        </body>
                        </html>";

                    await SendEmailAsync(user.Email, "Account Locked", emailBody);
                }

                await _context.SaveChangesAsync();
                return AuthResult.Failure("Invalid credentials");
            }

            user.FailedLoginAttempts = 0;
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return AuthResult.Success(user, GenerateJwtToken(user));
        }

        public async Task<AuthResult> ChangePasscodeAsync(string policeId, string currentPasscode, string newPasscode)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PoliceId == policeId);

            if (user == null)
                return AuthResult.Failure("User not found");

            if (!BCrypt.Net.BCrypt.Verify(currentPasscode, user.PasscodeHash))
                return AuthResult.Failure("Current passcode is incorrect");

            user.PasscodeHash = BCrypt.Net.BCrypt.HashPassword(newPasscode);
            user.LastPasswordChange = DateTime.UtcNow;
            user.RequirePasswordReset = false;

            try
            {
                await _context.SaveChangesAsync();

                var emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; color: #000000;'>
                        <h2>Passcode Changed</h2>
                        <p>Dear {user.FirstName},</p>
                        <p>Your passcode has been successfully changed. If you did not request this change, please contact support immediately.</p>
                        <p>Best regards,<br>CaseRelay Team</p>
                    </body>
                    </html>";

                await SendEmailAsync(user.Email, "Passcode Changed", emailBody);

                return AuthResult.Success(user);
            }
            catch (Exception)
            {
                return AuthResult.Failure("Passcode change failed");
            }
        }

        public async Task<AuthResult> UnlockAccountAsync(string policeId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PoliceId == policeId);

            if (user == null)
                return AuthResult.Failure("User not found");

            user.IsActive = true;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            try
            {
                await _context.SaveChangesAsync();

                var emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; color: #000000;'>
                        <h2>Account Unlocked</h2>
                        <p>Dear {user.FirstName},</p>
                        <p>Your account has been unlocked and is now active. You can log in and continue using CaseRelay.</p>
                        <p>Best regards,<br>CaseRelay Team</p>
                    </body>
                    </html>";

                await SendEmailAsync(user.Email, "Account Unlocked", emailBody);

                return AuthResult.Success(user);
            }
            catch (Exception)
            {
                return AuthResult.Failure("Failed to unlock account");
            }
        }

        private string GenerateJwtToken(User user)
        {
            var secretKey = _configuration["Jwt:SecretKey"] ?? "defaultSecretKey";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] 
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.PoliceId),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, user.Role ?? "Unknown"),
                new Claim("department", user.Department ?? "Unknown"),
                new Claim("userId", user.UserID.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "defaultIssuer",
                audience: _configuration["Jwt:Audience"] ?? "defaultAudience",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; }
        public string SenderPassword { get; set; }
        public string FromName { get; set; }
    }
}
