using System.ComponentModel.DataAnnotations;

namespace SB.DTOs
{
    public class VerifyEmailRequest
    {
        //[Required(ErrorMessage = "Email is required")]
        //[EmailAddress(ErrorMessage = "Invalid email format")]
        //public string Email { get; set; }

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "OTP must be 4 digits")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "OTP must be numeric")]
        public string Otp { get; set; }
    }
}
