using System;

namespace CaseRelayAPI.Models
{
    public class Login
    {
        public string PoliceId { get; set; } = string.Empty;
        public string Passcode { get; set; } = string.Empty;
    }
}
