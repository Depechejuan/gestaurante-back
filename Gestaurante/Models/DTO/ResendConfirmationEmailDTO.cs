using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class ResendConfirmationEmailDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
