using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class ClienteResendCodeDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
