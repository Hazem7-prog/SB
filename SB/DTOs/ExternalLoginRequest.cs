namespace SB.DTOs
{
    public class ExternalLoginRequest
    {
        public string Provider { get; set; } // Google | Facebook
        public string Token { get; set; }

    }
}
