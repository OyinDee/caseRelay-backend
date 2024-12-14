using System;

namespace CaseRelayAPI.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string PoliceId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? BadgeNumber { get; set; }
        public string? Rank { get; set; }
        public string? Department { get; set; }
        public string? Division { get; set; }
        public string? Precinct { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? MobilePhone { get; set; }
        public string? WorkPhone { get; set; }
        public string PasscodeHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Clearance { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public DateTime? LastPasswordChange { get; set; }
        public string? Station { get; set; }
        public string? SpecialUnit { get; set; }
        public string? PersonalIdentificationNumber { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? SupervisorId { get; set; }
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }
        public bool RequirePasswordReset { get; set; } = false;
    }
}
