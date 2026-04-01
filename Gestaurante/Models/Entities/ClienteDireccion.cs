using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class ClienteDireccion
    {
        [Key]
        [Required]
        public Guid IdClienteDireccion { get; set; }

        [Required]
        public Guid IdUsuarioCliente { get; set; }

        [Required]
        [MaxLength(80)]
        public string Alias { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Street { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Province { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Notes { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
