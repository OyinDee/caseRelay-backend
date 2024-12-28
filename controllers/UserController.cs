using Microsoft.AspNetCore.Mvc;
using CaseRelayAPI.Services;
using CaseRelayAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace CaseRelayAPI.Controllers
{
    /// <summary>
    /// Controller for managing user-related operations
    /// </summary>
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

        /// <summary>
        /// Gets all users in the system.
        /// </summary>
        /// <returns>A list of all users.</returns>
        [HttpGet("all")]
        [Authorize(Roles = "Admin")] // Optional: Restricts access to admins
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();

            if (users == null || users.Count == 0)
                return NotFound(new { message = "No users found." });

            return Ok(users);
        }

        /// <summary>
        /// Gets the profile of the logged-in user.
        /// </summary>
        /// <returns>The profile of the logged-in user.</returns>
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

        /// <summary>
        /// Gets the profile of a user by user ID.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The profile of the user.</returns>
        [HttpGet("{userId}")]
        [Authorize]
        public async Task<IActionResult> GetUser(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(user);
        }

        /// <summary>
        /// Gets all cases associated with a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A list of cases associated with the user.</returns>
        [HttpGet("{userId}/cases")]
        [Authorize]
        public async Task<IActionResult> GetCasesForUser(int userId)
        {
            var cases = await _caseService.GetCasesByUserIdAsync(userId);

            if (cases == null || cases.Count == 0)
                return NotFound(new { message = "No cases found for the specified user." });

            return Ok(cases);
        }

        /// <summary>
        /// Updates the profile of the logged-in user.
        /// </summary>
        /// <param name="user">The updated user profile.</param>
        /// <returns>The updated user profile.</returns>
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

        /// <summary>
        /// Changes the role of a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="newRole">The new role for the user.</param>
        /// <returns>The updated user profile.</returns>
        [HttpPut("change-role/{userId}")]
        [Authorize(Roles = "Admin")]
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

        /// <summary>
        /// Promotes a user to admin.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The updated user profile.</returns>
        [HttpPut("promote-to-admin/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PromoteToAdmin(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            user.Role = "Admin";
            var updatedUser = await _userService.UpdateUserAsync(user, isAdminOperation: true);

            if (updatedUser == null)
                return BadRequest(new { message = "Failed to promote user to admin." });

            return Ok(new { message = "User promoted to admin successfully.", user = updatedUser });
        }

        /// <summary>
        /// Deletes a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        [HttpDelete("delete/{userId}")]
        [Authorize(Roles = "Admin")] // Only admins can delete users
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var result = await _userService.DeleteUserAsync(userId);
            if (!result)
                return BadRequest(new { message = "Failed to delete user." });
            
            return Ok(new { message = "User deleted successfully." });
        }

        /// <summary>
        /// Creates a new user.
        /// </summary>
        /// <param name="newUser">The new user to create.</param>
        /// <returns>The created user profile.</returns>
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
