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
using System.Security.Cryptography;
using CaseRelayAPI.Dtos;

namespace CaseRelayAPI.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, EmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<AuthResult> InitiateForgotPasswordAsync(string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                    return AuthResult.Failure("No user found with this email.");

                var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                user.PasswordResetToken = resetToken;
                user.ResetTokenExpiration = DateTime.UtcNow.AddHours(1);

                await _context.SaveChangesAsync();

                var resetLink = $"http://localhost:3000/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(email)}";
                await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

                return AuthResult.Success(user);
            }
            catch (Exception ex)
            {
                return AuthResult.Failure("Failed to process password reset request.");
            }
        }

        public async Task<AuthResult> ResetPasswordAsync(string email, string resetToken, string newPasscode)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                    return AuthResult.Failure("No user found with this email.");

                if (string.IsNullOrEmpty(user.PasswordResetToken) || user.PasswordResetToken != resetToken || user.ResetTokenExpiration < DateTime.UtcNow)
                    return AuthResult.Failure("Invalid or expired reset token.");

                user.PasscodeHash = BCrypt.Net.BCrypt.HashPassword(newPasscode);
                user.PasswordResetToken = null;
                user.ResetTokenExpiration = null;
                user.LastPasswordChange = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; color: #000000;'>
                    <h2>Password Successfully Reset</h2>
                    <p>Dear {user.FirstName},</p>
                    <p>Your password has been successfully reset. If you did not make this change, please contact support immediately.</p>
                    <p>Best regards,<br>CaseRelay Team</p>
                </body>
                </html>";

                await _emailService.SendEmailAsync(user.Email, "Password Reset Confirmation", emailBody);

                return AuthResult.Success(user);
            }
            catch (Exception ex)
            {
                return AuthResult.Failure("Failed to reset password.");
            }
        }

        public async Task<AuthResult> RegisterUserAsync(User user, string passcode)
        {
            if (!IsValidEmail(user.Email))
                return AuthResult.Failure("Invalid email format.");

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.PoliceId == user.PoliceId || u.Email == user.Email);

            if (existingUser != null)
                return AuthResult.Failure("User with this Police ID or email already exists");

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

                await _emailService.SendEmailAsync(user.Email, "Welcome to CaseRelay!", emailBody);

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

                    await _emailService.SendEmailAsync(user.Email, "Account Locked", emailBody);
                }

                await _context.SaveChangesAsync();
                return AuthResult.Failure("Invalid credentials");
            }

            user.FailedLoginAttempts = 0;
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return AuthResult.Success(user, GenerateJwtToken(user));
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
                new Claim(ClaimTypes.Role, user.Role ?? "Unknown"), // Role claim
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

        public async Task<Result> ChangePasscodeAsync(string policeId, string currentPasscode, string newPasscode)
        {
            if (!IsPasscodeStrong(newPasscode))
            {
                return new Result { IsSuccess = false, ErrorMessage = "New passcode does not meet strength requirements." };
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.PoliceId == policeId);
            if (user == null)
            {
                return new Result { IsSuccess = false, ErrorMessage = "User not found." };
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPasscode, user.PasscodeHash))
            {
                return new Result { IsSuccess = false, ErrorMessage = "Current passcode is incorrect." };
            }

            user.PasscodeHash = BCrypt.Net.BCrypt.HashPassword(newPasscode);
            await _context.SaveChangesAsync();

            return new Result { IsSuccess = true };
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsPasscodeStrong(string passcode)
        {
            return passcode.Length >= 8 &&
                   passcode.Any(char.IsUpper) &&
                   passcode.Any(char.IsLower) &&
                   passcode.Any(char.IsDigit) &&
                   passcode.Any(ch => !char.IsLetterOrDigit(ch));
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Result> UpdateUserProfileAsync(string policeId, UserUpdateDto updateDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PoliceId == policeId);
            if (user == null)
            {
                return new Result { IsSuccess = false, ErrorMessage = "User not found." };
            }

            user.FirstName = updateDto.FirstName ?? user.FirstName;
            user.LastName = updateDto.LastName ?? user.LastName;
            user.BadgeNumber = updateDto.BadgeNumber ?? user.BadgeNumber;
            user.Rank = updateDto.Rank ?? user.Rank;
            user.Department = updateDto.Department ?? user.Department;
            user.Phone = updateDto.Phone ?? user.Phone;

            if (updateDto.Email != null && updateDto.Email != user.Email)
            {
                if (!IsValidEmail(updateDto.Email))
                {
                    return new Result { IsSuccess = false, ErrorMessage = "Invalid email format." };
                }
                if (await _context.Users.AnyAsync(u => u.Email == updateDto.Email))
                {
                    return new Result { IsSuccess = false, ErrorMessage = "Email already in use." };
                }
                user.Email = updateDto.Email;
            }

            await _context.SaveChangesAsync();
            return new Result { IsSuccess = true };
        }

        public async Task<Result> DeleteUserAccountAsync(string policeId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PoliceId == policeId);
            if (user == null)
            {
                return new Result { IsSuccess = false, ErrorMessage = "User not found." };
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return new Result { IsSuccess = true };
        }
    }
}

