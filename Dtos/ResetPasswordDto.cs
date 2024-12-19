namespace CaseRelayAPI.Dtos
{
    public class ForgotPasswordDto
    {
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; }
        public string ResetToken { get; set; }
        public string NewPasscode { get; set; }
    }
}
