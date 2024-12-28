namespace CaseRelayAPI.Utilities
{
    public static class PasswordGenerator
    {
        public static string GenerateTemporaryPassword()
        {
            Random random = new Random();
            return random.Next(10000, 99999).ToString();
        }
    }
}
