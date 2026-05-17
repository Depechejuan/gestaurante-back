using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class ConfirmEmailByTokenDTO
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
