//using System.ComponentModel.DataAnnotations;
//using System.Linq;
//using Microsoft.EntityFrameworkCore;
//using SB.Models;

//namespace SB.Validation
//{
//    /// <summary>
//    /// Validates at model-binding time that the provided phone number does not already exist.
//    /// Uses ValidationContext.GetService to obtain SBDbContext from DI.
//    /// NOTE: This validation is synchronous. It prevents the most common duplicate cases at request time.
//    /// Service-level checks must still be done to avoid race conditions.
//    /// </summary>
//    public class UniquePhoneAttribute : ValidationAttribute
//    {
//        //public UniquePhoneAttribute()
//        //{
//        //    ErrorMessage = "This phone number is already used.";
//        //}

//        //protected override ValidationResult IsValid(object value, ValidationContext validationContext)
//        //{
//        //    var phone = value as string;
//        //    var phoneNorm = NormalizePhone(phone);

//        //    if (string.IsNullOrWhiteSpace(phoneNorm))
//        //        return ValidationResult.Success;

//        //    var db = validationContext.GetService(typeof(SBDbContext)) as SBDbContext;
//        //    if (db == null)
//        //        return ValidationResult.Success;

//        //    // Load only non-deleted phone values and compare using same normalization
//        //    var phonesInDb = db.Children
//        //        .AsNoTracking()
//        //        .Where(c => !c.IsDeleted && c.SimCardNum != null)
//        //        .Select(c => c.SimCardNum)
//        //        .ToList();

//        //    foreach (var dbPhone in phonesInDb)
//        //    {
//        //        if (NormalizePhone(dbPhone) == phoneNorm)
//        //            return new ValidationResult(ErrorMessage);
//        //    }

//        //    return ValidationResult.Success;
//        //}

//        //private static string NormalizePhone(string? input)
//        //{
//        //    if (string.IsNullOrWhiteSpace(input)) return string.Empty;

//        //    // Keep same canonicalization as service: digits-only, remove leading "00"
//        //    var digits = new string(input.Where(char.IsDigit).ToArray());
//        //    if (digits.StartsWith("00")) digits = digits.Substring(2);
//        //    return digits;
//        }
//    }
//}   