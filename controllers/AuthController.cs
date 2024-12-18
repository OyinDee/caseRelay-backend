using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CaseRelayAPI.Services;
using CaseRelayAPI.Models;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using CaseRelayAPI.Dtos;

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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Passcode) || string.IsNullOrEmpty(request.PoliceId))
                return BadRequest(new { message = "Invalid registration details." });

            var user = new User
            {
                PoliceId = request.PoliceId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                BadgeNumber = request.BadgeNumber,
                Rank = request.Rank,
                Department = request.Department,
                Email = request.Email,
                Phone = request.Phone,
                Role = string.IsNullOrWhiteSpace(request.Role) ? "Officer" : request.Role
            };

            var result = await _authService.RegisterUserAsync(user, request.Passcode);

            return result.IsSuccess
                ? Ok(new { message = "Registration successful", userId = result.User.UserID })
                : BadRequest(new { message = result.ErrorMessage });
        }

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

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email)) 
                return BadRequest(new { message = "Email is required." });

            var result = await _authService.InitiateForgotPasswordAsync(request.Email);
            
            return result.IsSuccess ? Ok(new { message = "Password reset instructions sent to your email." })
                : BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.ResetToken) || string.IsNullOrEmpty(request.NewPasscode)) 
                return BadRequest(new { message = "Invalid reset details." });

            var result = await _authService.ResetPasswordAsync(request.Email, request.ResetToken, request.NewPasscode);
            
            return result.IsSuccess ? Ok(new { message = "Password reset successfully." }) 
                : BadRequest(new { message = result.ErrorMessage });
        }

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

