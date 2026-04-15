using SB.Models.Enum;
using SB.Validation;
using System.ComponentModel.DataAnnotations;

namespace SB.DTOs
{
    public class ChildUpdate
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [Range(0, 18, ErrorMessage = "Child should be between 0-18")]
        public int Age { get; set; }

        [Required]
        public Gender Gender { get; set; }

        //[Required]
        //[EgyptianPhone]
        // don't use UniquePhone attribute here because we need to ignore the current entity id.
        // we'll validate uniqueness inside the service (server-side)
        public string? SimCardNum { get; set; }

        public string? Image { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
