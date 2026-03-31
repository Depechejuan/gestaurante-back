using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class ClienteRegisterDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }
}
