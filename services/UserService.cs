using CaseRelayAPI.Models;
using CaseRelayAPI.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CaseRelayAPI.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public UserService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService
        )
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        private string GetUserIdFromToken()
        {
            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token)) return string.Empty;

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
                if (string.IsNullOrEmpty(userId)) return null;

                var existingUser = await _context.Users.FindAsync(user.UserID);
                if (existingUser == null || existingUser.UserID.ToString() != userId) return null;
            }

            var existingUserForUpdate = await _context.Users.FindAsync(user.UserID);
            if (existingUserForUpdate == null) return null;

            existingUserForUpdate.Role = user.Role ?? existingUserForUpdate.Role;
            existingUserForUpdate.FirstName = user.FirstName ?? existingUserForUpdate.FirstName;
            existingUserForUpdate.LastName = user.LastName ?? existingUserForUpdate.LastName;
            existingUserForUpdate.Email = user.Email ?? existingUserForUpdate.Email;
            existingUserForUpdate.Phone = user.Phone ?? existingUserForUpdate.Phone;
            existingUserForUpdate.Rank = user.Rank ?? existingUserForUpdate.Rank;

            _context.Users.Update(existingUserForUpdate);
            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = existingUserForUpdate.UserID,
                Title = "Profile Updated",
                Message = "Your profile information has been updated",
                Type = "admin"
            });

            return existingUserForUpdate;
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

                await _notificationService.CreateNotificationAsync(new Notification
                {
                    UserId = userId,
                    Title = "Account Deleted",
                    Message = "Your account has been deleted",
                    Type = "admin"
                });

                var cases = await _context.Cases
                    .Where(c => c.AssignedOfficerId == user.PoliceId)
                    .ToListAsync();

                foreach (var caseItem in cases)
                {
                    caseItem.AssignedOfficerId = "Unassigned";
                    caseItem.PreviousOfficerId = user.PoliceId;
                }

                _context.Cases.UpdateRange(cases);
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch
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

            return newUser;
        }
    }
} 
