using System.ComponentModel.DataAnnotations;

namespace SB.Validation
{
    public class PasswordValidation : ValidationAttribute
    {
                protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("Password is required.");
            }

            string password = value.ToString();

            if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            {
                return new ValidationResult("Password must be at least 8 characters long and contain uppercase letters, lowercase letters, and numbers.");
            }

            return ValidationResult.Success;
        }
    }
}
