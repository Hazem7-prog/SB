using SB.Models.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SB.Models
{
    public class Child
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ChildId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Range(0, 18)]
        public int Age { get; set; }

        public Gender Gender { get; set; }

        [MaxLength(32)]
        public string? SimCardNum { get; set; }

        public string? Image { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        // Make UserId nullable to allow anonymous creation
        public string? UserId { get; set; }

        // Navigation property optional
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
