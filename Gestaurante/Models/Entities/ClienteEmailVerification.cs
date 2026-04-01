using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class ClienteEmailVerification
    {
        [Key]
        [Required]
        public Guid IdClienteEmailVerification { get; set; }

        [Required]
        public Guid IdUsuarioCliente { get; set; }

        [Required]
        [MaxLength(128)]
        public string CodeHash { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime? ConsumedAt { get; set; }

        [Required]
        public int AttemptCount { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
