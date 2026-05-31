using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class ContactMessageDTO
    {
        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(160)]
        public string Email { get; set; } = string.Empty;

        [StringLength(40)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(160)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string Message { get; set; } = string.Empty;
    }
}
