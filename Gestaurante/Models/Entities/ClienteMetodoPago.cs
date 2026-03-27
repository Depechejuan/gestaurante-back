using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class ClienteMetodoPago
    {
        [Key]
        [Required]
        public Guid IdClienteMetodoPago { get; set; }

        [Required]
        public Guid IdUsuarioCliente { get; set; }

        [Required]
        [MaxLength(120)]
        public string MockPaymentToken { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string Brand { get; set; } = string.Empty;

        [Required]
        [MaxLength(4)]
        public string Last4 { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string HolderName { get; set; } = string.Empty;

        [Required]
        public int ExpMonth { get; set; }

        [Required]
        public int ExpYear { get; set; }

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
