using Microsoft.AspNetCore.Mvc;
using CaseRelayAPI.Services;
using CaseRelayAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;


namespace CaseRelayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ICaseService _caseService; 
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ICaseService caseService, ILogger<UserController> logger)
        {
            _userService = userService;
            _caseService = caseService;
            _logger = logger;
        }

        // Method to get the profile of the logged-in user
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var policeId = User.FindFirst(c => c.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(policeId))
                return Unauthorized(new { message = "User identity could not be verified." });

            var user = await _userService.GetUserByPoliceIdAsync(policeId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(user);
        }

        // Method to get user profile by user ID
        [HttpGet("{userId}")]
        [Authorize]
        public async Task<IActionResult> GetUser(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(user);
        }

        // Method to get all cases associated with a user
        [HttpGet("{userId}/cases")]
        [Authorize]
        public async Task<IActionResult> GetCasesForUser(int userId)
        {
            var cases = await _caseService.GetCasesByUserIdAsync(userId);

            if (cases == null || cases.Count == 0)
                return NotFound(new { message = "No cases found for the specified user." });

            return Ok(cases);
        }

        // Method to update the user profile
        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] User user)
        {
            var policeId = User.FindFirst(c => c.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(policeId))
                return Unauthorized(new { message = "User identity could not be verified." });

            var existingUser = await _userService.GetUserByPoliceIdAsync(policeId);
            if (existingUser == null)
                return NotFound(new { message = "User not found." });

            existingUser.FirstName = user.FirstName ?? existingUser.FirstName;
            existingUser.LastName = user.LastName ?? existingUser.LastName;
            existingUser.Email = user.Email ?? existingUser.Email;
            existingUser.Phone = user.Phone ?? existingUser.Phone;
            existingUser.Rank = user.Rank ?? existingUser.Rank;

            var updatedUser = await _userService.UpdateUserAsync(existingUser);

            if (updatedUser == null)
                return BadRequest(new { message = "Failed to update user profile." });

            return Ok(updatedUser);
        }

        // Method for an admin to change a user's role
        [HttpPut("change-role/{userId}")]
        [Authorize(Roles = "Admin")] // This checks if the user has the "Admin" role
        public async Task<IActionResult> ChangeUserRole(int userId, [FromBody] string newRole)
        {
            if (string.IsNullOrEmpty(newRole))
                return BadRequest(new { message = "Invalid role." });

            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            user.Role = newRole;
            var updatedUser = await _userService.UpdateUserAsync(user);

            return Ok(updatedUser);
        }

        // Method for an admin to change a user's role to admin
        [HttpPut("promote-to-admin/{userId}")]
        [Authorize(Roles = "Admin")] // This checks if the user has the "Admin" role
        public async Task<IActionResult> PromoteToAdmin(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            user.Role = "Admin";
            var updatedUser = await _userService.UpdateUserAsync(user);

            if (updatedUser == null)
                return BadRequest(new { message = "Failed to promote user to admin." });

            return Ok(new { message = "User promoted to admin successfully.", user = updatedUser });
        }

        // Method for an admin to delete a user
        [HttpDelete("delete/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            var result = await _userService.DeleteUserAsync(userId);

            if (!result)
                return BadRequest(new { message = "Failed to delete user." });

            return Ok(new { message = "User deleted successfully." });
        }

        // Method to create a new user
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] User newUser)
        {
            var createdUser = await _userService.CreateUserAsync(newUser);

            if (createdUser == null)
                return BadRequest(new { message = "Failed to create user." });

            return Ok(createdUser);
        }
    }
}
