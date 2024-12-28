using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CaseRelayAPI.Services;
using CaseRelayAPI.Models;

namespace CaseRelayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserNotifications()
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(notifications);
        }

        [HttpPatch("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var success = await _notificationService.MarkNotificationAsReadAsync(notificationId);
            if (!success) return NotFound(new { message = "Notification not found." });
            return Ok(new { message = "Notification marked as read." });
        }

        [HttpDelete("{notificationId}")]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            var success = await _notificationService.DeleteNotificationAsync(notificationId);
            if (!success) return NotFound(new { message = "Notification not found." });
            return Ok(new { message = "Notification deleted." });
        }
    }
}
