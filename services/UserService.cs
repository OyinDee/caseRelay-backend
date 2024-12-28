using CaseRelayAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CaseRelayAPI.Data;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace CaseRelayAPI.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService; 

        public UserService(ApplicationDbContext context, 
                          IHttpContextAccessor httpContextAccessor,
                          INotificationService notificationService,
                          IEmailService emailService) 
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
            _emailService = emailService; 
        }
        private string GetUserIdFromToken()
        {
            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token))
                return string.Empty;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
            var userIdClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == "userId");

            return userIdClaim?.Value;
        }

        public async Task<User?> GetUserByPoliceIdAsync(string policeId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.PoliceId == policeId);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User?> UpdateUserAsync(User user, bool isAdminOperation = false)
        {
            if (!isAdminOperation)
            {
                var userId = GetUserIdFromToken();
                if (string.IsNullOrEmpty(userId))
                    return null;

                var existingUser = await _context.Users.FindAsync(user.UserID);
                if (existingUser == null || existingUser.UserID.ToString() != userId)
                    return null;
            }
            else
            {
                var existingUser = await _context.Users.FindAsync(user.UserID);
                if (existingUser == null)
                    return null;

                existingUser.Role = user.Role ?? existingUser.Role;
                existingUser.FirstName = user.FirstName ?? existingUser.FirstName;
                existingUser.LastName = user.LastName ?? existingUser.LastName;
                existingUser.Email = user.Email ?? existingUser.Email;
                existingUser.Phone = user.Phone ?? existingUser.Phone;
                existingUser.Rank = user.Rank ?? existingUser.Rank;

                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                // Notify user about profile update
                await _notificationService.CreateNotificationAsync(new Notification
                {
                    UserId = existingUser.UserID,
                    Title = "Profile Updated",
                    Message = "Your profile information has been updated",
                    Type = "admin"
                });

                return existingUser;
            }

            return null;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                // Notify user about account deletion
                await _notificationService.CreateNotificationAsync(new Notification
                {
                    UserId = userId,
                    Title = "Account Deleted",
                    Message = "Your account has been deleted",
                    Type = "admin"
                });

                // First, handle related cases
                var cases = await _context.Cases
                    .Where(c => c.AssignedOfficerId == user.PoliceId)
                    .ToListAsync();

                foreach (var caseItem in cases)
                {
                    caseItem.AssignedOfficerId = "Unassigned";
                    caseItem.PreviousOfficerId = user.PoliceId;
                }

                // Update the cases
                _context.Cases.UpdateRange(cases);

                // Now delete the user
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<User?> CreateUserAsync(User newUser)
        {
            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            return newUser;
        }

        public async Task<User?> CreateUserWithPasswordAsync(User newUser, string temporaryPassword)
        {
            newUser.PasscodeHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.IsActive = true;
            newUser.RequirePasswordReset = true;
            
            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            // Send credentials email
            await _emailService.SendNewUserCredentialsAsync(
                newUser.Email,
                newUser.FirstName,
                newUser.PoliceId,
                temporaryPassword
            );

            return newUser;
        }
    }
}
