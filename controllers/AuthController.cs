using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CaseRelayAPI.Services;
using CaseRelayAPI.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CaseRelayAPI.Controllers
{
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
        /// Authenticates a user and generates a JWT token.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            if (string.IsNullOrEmpty(request.PoliceId) || string.IsNullOrEmpty(request.Passcode))
                return BadRequest(new { message = "Invalid login details." });

            _logger.LogInformation("Authenticating user with PoliceId: {PoliceId}", request.PoliceId);

            var authResult = await _authService.AuthenticateAsync(request.PoliceId, request.Passcode);

            if (!authResult.IsSuccess)
            {
                _logger.LogError("Authentication failed for PoliceId: {PoliceId} with error: {ErrorMessage}", 
                                 request.PoliceId, authResult.ErrorMessage);

                return Unauthorized(new { message = authResult.ErrorMessage });
            }

            _logger.LogInformation("User authenticated successfully with PoliceId: {PoliceId}", request.PoliceId);

            return Ok(new
            {
                token = authResult.Token,
                userId = authResult.User.UserID,
                policeId = authResult.User.PoliceId,
                name = $"{authResult.User.FirstName} {authResult.User.LastName}",
                role = authResult.User.Role,
                department = authResult.User.Department,
                badgeNumber = authResult.User.BadgeNumber,
                rank = authResult.User.Rank
            });
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var user = new User
            {
                PoliceId = request.PoliceId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Role = request.Role,
                Department = request.Department,
                BadgeNumber = request.BadgeNumber,
                Rank = request.Rank
            };

            var result = await _authService.RegisterUserAsync(user, request.Passcode);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new { message = "Registration successful" });
        }

        /// <summary>
        /// Initiates the forgot password process.
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest(new { message = "Email is required." });

            var result = await _authService.InitiateForgotPasswordAsync(request.Email);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new { message = "Password reset instructions sent to your email." });
        }

        /// <summary>
        /// Resets a user's password.
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.ResetToken) || string.IsNullOrEmpty(request.NewPasscode))
                return BadRequest(new { message = "Invalid reset details." });

            var result = await _authService.ResetPasswordAsync(request.Email, request.ResetToken, request.NewPasscode);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new { message = "Password reset successfully." });
        }

        /// <summary>
        /// Changes a user's passcode.
        /// </summary>
        [Authorize]
        [HttpPost("change-passcode")]
        public async Task<IActionResult> ChangePasscode([FromBody] ChangePasscodeDto request)
        {
            var policeId = User.FindFirst(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(policeId))
                return Unauthorized(new { message = "User identity could not be verified." });

            if (string.IsNullOrEmpty(request.CurrentPasscode) || string.IsNullOrEmpty(request.NewPasscode))
                return BadRequest(new { message = "Invalid passcode details." });

            var result = await _authService.ChangePasscodeAsync(policeId, request.CurrentPasscode, request.NewPasscode);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new { message = "Passcode changed successfully" });
        }

        /// <summary>
        /// Retrieves the authenticated user's profile.
        /// </summary>
        [Authorize]
        [HttpGet("userinfo")]
        public async Task<IActionResult> GetUserInfo()
        {
            var policeId = User.FindFirst(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(policeId))
                return Unauthorized(new { message = "User identity could not be verified." });

            var user = await _authService.GetUserByPoliceIdAsync(policeId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(new
            {
                userId = user.UserID,
                policeId = user.PoliceId,
                name = $"{user.FirstName} {user.LastName}",
                role = user.Role,
                department = user.Department,
                badgeNumber = user.BadgeNumber,
                rank = user.Rank
            });
        }

        /// <summary>
        /// Updates the authenticated user's profile.
        /// </summary>
        [Authorize]
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateDto request)
        {
            var policeId = User.FindFirst(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(policeId))
                return Unauthorized(new { message = "User identity could not be verified." });

            if (request == null)
                return BadRequest(new { message = "Invalid update details." });

            var result = await _authService.UpdateUserProfileAsync(policeId, request);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new { message = "Profile updated successfully." });
        }

        /// <summary>
        /// Deletes the authenticated user's account.
        /// </summary>
        [Authorize]
        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount()
        {
            var policeId = User.FindFirst(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(policeId))
                return Unauthorized(new { message = "User identity could not be verified." });

            var result = await _authService.DeleteUserAccountAsync(policeId);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new { message = "Account deleted successfully." });
        }
    }
}
