using CaseRelayAPI.Data;
using CaseRelayAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseRelayAPI.Services
{
    public interface INotificationService
    {
        Task<List<Notification>> GetUserNotificationsAsync(int userId);
        Task<bool> CreateNotificationAsync(Notification notification);
        Task<bool> MarkNotificationAsReadAsync(int notificationId);
        Task<bool> DeleteNotificationAsync(int notificationId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public NotificationService(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> CreateNotificationAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Get user email
            var user = await _context.Users.FindAsync(notification.UserId);
            if (user != null)
            {
                // Send email notification
                var emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; background-color: #ffffff; color: #000000; padding: 20px;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                        <h2 style='color: #000000;'>{notification.Title}</h2>
                        <p style='color: #000000;'>{notification.Message}</p>
                        <hr style='border: 1px solid #e0e0e0;'>
                        <p style='color: #666666; font-size: 12px;'>
                            This is an automated notification from CaseRelay.
                            {(notification.RelatedCaseId != null ? $"Related Case ID: {notification.RelatedCaseId}" : "")}
                        </p>
                    </div>
                </body>
                </html>";

                await _emailService.SendEmailAsync(user.Email, notification.Title, emailBody);
            }

            return true;
        }

        public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null) return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null) return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
