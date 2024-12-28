namespace CaseRelayAPI.Dtos
{
    public class CreateUserDto
    {
        public string PoliceId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public string Department { get; set; }
        public string BadgeNumber { get; set; }
        public string Rank { get; set; }
    }
}
