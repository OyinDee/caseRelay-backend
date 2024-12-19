namespace CaseRelayAPI.Models
{
    public class EmailSettings
    {
        // SMTP Server address (e.g., smtp.gmail.com)
        public string SmtpServer { get; set; } = string.Empty;
        
        // Port number (typically 587 for TLS or 465 for SSL)
        public int Port { get; set; }
        
        // Sender email address, e.g., case.relay0@gmail.com
        public string SenderEmail { get; set; } = string.Empty;
        
        // Name that will appear as the sender in emails
        public string SenderName { get; set; } = string.Empty;
        
        // Email account password for sender (consider storing this securely)
        public string SenderPassword { get; set; } = string.Empty;
        
        // Use SSL or not for secure communication
        public bool UseSsl { get; set; } = true;
    }
}
