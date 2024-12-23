using CaseRelayAPI.Models;
using System.Threading.Tasks;

namespace CaseRelayAPI.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByPoliceIdAsync(string policeId);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int userId);
        Task<User?> CreateUserAsync(User newUser);
    }
}
