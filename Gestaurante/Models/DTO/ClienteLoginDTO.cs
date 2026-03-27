using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class ClienteLoginDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
