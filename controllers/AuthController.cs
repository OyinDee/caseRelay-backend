using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CaseRelayAPI.Services;
using CaseRelayAPI.Models;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using CaseRelayAPI.Dtos;

namespace CaseRelayAPI.Controllers
{
    /// <summary>
    /// Controller for managing authentication-related operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="request">The user registration details.</param>
        /// <returns>A message indicating the result of the registration.</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Get requesting user's ID from token if it exists
            string? requestingUserId = User.FindFirst("userId")?.Value;

            var user = new User
            {
                PoliceId = request.PoliceId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Role = request.Role, // This will be overridden for non-admin users
                Department = request.Department,
                BadgeNumber = request.BadgeNumber,
                Rank = request.Rank
            };

            var result = await _authService.RegisterUserAsync(user, request.Passcode, requestingUserId);

            if (!result.Success)
                return BadRequest(new { message = result.Error });

            return Ok(new { message = "Registration successful" });
        }

        /// <summary>
        /// Authenticates a user and generates a JWT token.
        /// </summary>
        /// <param name="request">The user login details.</param>
        /// <returns>The JWT token and user details.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.PoliceId) || string.IsNullOrEmpty(request.Passcode))
                return BadRequest(new { message = "Invalid login details." });

            _logger.LogInformation("Attempting to authenticate user with PoliceId: {PoliceId}", request.PoliceId);

            var result = await _authService.AuthenticateAsync(request.PoliceId, request.Passcode);

            if (result == null)
            {
                _logger.LogError("Authentication failed for PoliceId: {PoliceId}", request.PoliceId);
                return StatusCode(500, new { message = "An error occurred during login." });
            }

            if (!result.IsSuccess)
            {
                _logger.LogError("Authentication failed for PoliceId: {PoliceId} with error: {ErrorMessage}", request.PoliceId, result.ErrorMessage);
                return Unauthorized(new { message = result.ErrorMessage });
            }

            _logger.LogInformation("User authenticated successfully with PoliceId: {PoliceId}", request.PoliceId);

            var user = result.User;

            return Ok(new
            {
                token = result.Token,
                userId = user.UserID,
                name = $"{user.FirstName} {user.LastName}",
                role = user.Role,
                department = user.Department,
                badgeNumber = user.BadgeNumber,
                rank = user.Rank
            });
        }

        /// <summary>
        /// Initiates the forgot password process.
        /// </summary>
        /// <param name="request">The forgot password request details.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email)) 
                return BadRequest(new { message = "Email is required." });

            var result = await _authService.InitiateForgotPasswordAsync(request.Email);
            
            return result.IsSuccess ? Ok(new { message = "Password reset instructions sent to your email." })
                : BadRequest(new { message = result.ErrorMessage });
        }

        /// <summary>
        /// Resets the user's password.
        /// </summary>
        /// <param name="request">The reset password request details.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.ResetToken) || string.IsNullOrEmpty(request.NewPasscode)) 
                return BadRequest(new { message = "Invalid reset details." });

            var result = await _authService.ResetPasswordAsync(request.Email, request.ResetToken, request.NewPasscode);
            
            return result.IsSuccess ? Ok(new { message = "Password reset successfully." }) 
                : BadRequest(new { message = result.ErrorMessage });
        }

        /// <summary>
        /// Changes the user's passcode.
        /// </summary>
        /// <param name="request">The change passcode request details.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        [Authorize]
        [HttpPost("change-passcode")]
        public async Task<IActionResult> ChangePasscode([FromBody] ChangePasscodeDto request)
        {
            var policeId = User.FindFirst(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(policeId)) return Unauthorized(new { message = "User identity could not be verified." });
            if (request == null || string.IsNullOrEmpty(request.CurrentPasscode) || string.IsNullOrEmpty(request.NewPasscode))
                return BadRequest(new { message = "Invalid passcode details." });
            var result = await _authService.ChangePasscodeAsync(policeId, request.CurrentPasscode, request.NewPasscode);

            return result.IsSuccess
                ? Ok(new { message = "Passcode changed successfully" })
                : BadRequest(new { message = result.ErrorMessage });
        }

        /// <summary>
        /// Updates the user's profile.
        /// </summary>
        /// <param name="request">The user profile update details.</param>
        /// <returns>A message indicating the result of the operation.</returns>
        [Authorize]
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateDto request)
        {
            var policeId = User.FindFirst(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(policeId)) return Unauthorized(new { message = "User identity could not be verified." });
            if (request == null)
                return BadRequest(new { message = "Invalid update details." });

            var result = await _authService.UpdateUserProfileAsync(policeId, request);

            return result.IsSuccess
                ? Ok(new { message = "Profile updated successfully" })
                : BadRequest(new { message = result.ErrorMessage });
        }

        /// <summary>
        /// Deletes the user's account.
        /// </summary>
        /// <returns>A message indicating the result of the operation.</returns>
        [Authorize]
        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount()
        {
            var policeId = User.FindFirst(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(policeId)) return Unauthorized(new { message = "User identity could not be verified." });

            var result = await _authService.DeleteUserAccountAsync(policeId);

            return result.IsSuccess
                ? Ok(new { message = "Account deleted successfully" })
                : BadRequest(new { message = result.ErrorMessage });
        }
    }
}

public class RegisterRequest
{
    public string PoliceId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Passcode { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Department { get; set; }
    public string? BadgeNumber { get; set; }
    public string? Rank { get; set; }
}

