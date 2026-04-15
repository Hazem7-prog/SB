using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SB.Models
{
    public class User : IdentityUser    {
        [Key]

        
        //public Guid UserId { get; set; } = Guid.NewGuid();
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public byte[]? ProfileImage { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Provider { get; set; }    // Google | Facebook | Local
        public string? ProviderId { get; set; }  // sub / facebook id

        // OTP fields for password reset and email verification
        public string? OtpCodeHash { get; set; }
        public DateTime? OtpExpirationDate { get; set; }
        public int OtpFailedAttempts { get; set; } = 0;

        // Email verification
        public bool IsEmailVerified { get; set; } = false;

        // Track OTP verification state for password reset flow
        public bool IsPasswordResetOtpVerified { get; set; } = false;

        public virtual ICollection<Child> Children { get; set; } = new List<Child>();
    }
}
