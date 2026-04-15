using SB.Models.Enum;
using SB.Validation;
using System.ComponentModel.DataAnnotations;

namespace SB.DTOs
{
    public class ChildRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
            
        [Required]
        [Range(0,18, ErrorMessage ="Child should be between 0-18")]      
        public int Age { get; set; }

        [Required]
        public Gender Gender { get; set; }

        //[Required]
        //[EgyptianPhone]
        //[UniquePhone] // model-level uniqueness check (uses DbContext via DI)
        public string? SimCardNum { get; set; }

        public string? Image { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
