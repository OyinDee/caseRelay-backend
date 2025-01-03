using CaseRelayAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaseRelayAPI.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByPoliceIdAsync(string policeId);
        Task<User?> GetUserByIdAsync(int userId);
        Task<List<User>> GetAllUsersAsync();
        Task<User?> UpdateUserAsync(User user, bool isAdminOperation = false);
        Task<bool> DeleteUserAsync(int userId);
        Task<User?> CreateUserAsync(User newUser);
        Task<User?> CreateUserWithPasswordAsync(User newUser, string temporaryPassword);
    }
} 
