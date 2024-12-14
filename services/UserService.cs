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
    public class UserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
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

        public async Task<User?> UpdateUserAsync(User user)
        {
            var userId = GetUserIdFromToken();
            if (string.IsNullOrEmpty(userId))
                return null;

            var existingUser = await _context.Users.FindAsync(user.UserID);
            if (existingUser == null || existingUser.UserID.ToString() != userId)
                return null;

            existingUser.FirstName = user.FirstName ?? existingUser.FirstName;
            existingUser.LastName = user.LastName ?? existingUser.LastName;
            existingUser.Email = user.Email ?? existingUser.Email;
            existingUser.Phone = user.Phone ?? existingUser.Phone;
            existingUser.Rank = user.Rank ?? existingUser.Rank;

            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();

            return existingUser;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var userIdFromToken = GetUserIdFromToken();
            if (string.IsNullOrEmpty(userIdFromToken))
                return false;

            if (userIdFromToken != userId.ToString())
                return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<User?> CreateUserAsync(User newUser)
        {
            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            return newUser;
        }
    }
}
