namespace CaseRelayAPI.Models
{
    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public User User { get; set; } = new User();

        public static AuthResult Success(User user, string token = "")
        {
            return new AuthResult
            {
                IsSuccess = true,
                User = user,
                Token = token
            };
        }

        public static AuthResult Failure(string errorMessage)
        {
            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
