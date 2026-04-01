using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class CreateClienteMetodoPagoDTO
    {
        [Required]
        [MinLength(12)]
        public string CardNumber { get; set; } = string.Empty;
        [Required]
        public string HolderName { get; set; } = string.Empty;
        [Required]
        [Range(1, 12)]
        public int ExpMonth { get; set; }
        [Required]
        [Range(2024, 2100)]
        public int ExpYear { get; set; }
        public bool IsDefault { get; set; }
    }
}
