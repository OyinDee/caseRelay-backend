namespace CaseRelayAPI.Dtos
{
    public class ChangePasscodeDto
    {
        public string PoliceId { get; set; }
        public string CurrentPasscode { get; set; }
        public string NewPasscode { get; set; }
    }
}
