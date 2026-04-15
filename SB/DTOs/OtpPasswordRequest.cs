using System.ComponentModel.DataAnnotations;

namespace SB.DTOs
{
    public class OtpPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(4, MinimumLength = 4)]
        public string Otp { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }
    }
}
