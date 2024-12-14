using System.ComponentModel.DataAnnotations;

namespace CaseRelayAPI.Dtos
{
    public class UserRegistrationDto
    {
        [Required(ErrorMessage = "Police ID is required")]
        [StringLength(10, MinimumLength = 3, ErrorMessage = "Police ID must be between 3 and 10 characters")]
        public string PoliceId { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [StringLength(50, ErrorMessage = "First Name cannot exceed 50 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [StringLength(50, ErrorMessage = "Last Name cannot exceed 50 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Department is required")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Passcode is required")]
        [MinLength(6, ErrorMessage = "Passcode must be at least 6 characters long")]
        public string Passcode { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Badge Number is required")]
        [StringLength(20, ErrorMessage = "Badge Number cannot exceed 20 characters")]
        public string BadgeNumber { get; set; }

        [Required(ErrorMessage = "Rank is required")]
        public string Rank { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [StringLength(20, ErrorMessage = "Role name cannot exceed 20 characters")]
        public string Role { get; set; }
    }
}
