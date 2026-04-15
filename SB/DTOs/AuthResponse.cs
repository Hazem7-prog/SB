
namespace SB.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public UserResponse User { get; set; }
        public string Message { get; set; }

    }
}
