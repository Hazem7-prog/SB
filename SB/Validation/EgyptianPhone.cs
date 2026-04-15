using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SB.Validation
{
    public class EgyptianPhone : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return new ValidationResult("Phone number is required");

            string phone = value.ToString();

            var regex = new Regex(@"^01[0125][0-9]{8}$");

            if (!regex.IsMatch(phone))
            {
                return new ValidationResult("Invalid Egyptian phone number format.");
            }

            return ValidationResult.Success;
        }
    }
}
