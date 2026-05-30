using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class ResetPasswordDTO
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
