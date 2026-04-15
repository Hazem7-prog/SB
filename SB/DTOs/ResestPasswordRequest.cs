using System.ComponentModel.DataAnnotations;

namespace SB.DTOs
{
    public class ResestPasswordRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "OTP must be 4 digits")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "OTP must be numeric")]
        public string Otp { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

    }
}
