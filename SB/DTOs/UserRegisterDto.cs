using SB.Validation;
using System.ComponentModel.DataAnnotations;

namespace SB.DTOs
{
    public class UserRegisterDto
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email must not exceed 255 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        [PasswordValidation(ErrorMessage = "Password must be at least 8 characters long and contain uppercase letters, lowercase letters, and numbers.")]
        public string Password { get; set; }

        //[Required(ErrorMessage = "Confirm password is required")]
        //[Compare("Password", ErrorMessage = "Passwords do not match")]
        public string phone { get; set; }
       // public byte[]? ProfileImage { get; set; }

    }
}
