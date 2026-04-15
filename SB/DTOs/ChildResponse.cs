using SB.Models;
using SB.Models.Enum;

namespace SB.DTOs
{
    public class ChildResponse
    {
        public int ChildId { get; set; } // include DB id for CreatedAtAction
        public string Name { get; set; }
        public int Age { get; set; }
        public Gender Gender { get; set; }
        public string? SimCardNum { get; set; }
        public string? Image { get; set; }
        public string? Notes { get; set; }
    }
}
