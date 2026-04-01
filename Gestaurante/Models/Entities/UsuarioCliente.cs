using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class UsuarioCliente
    {
        [Key]
        [Required]
        public Guid IdUsuarioCliente { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(160)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(25)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(160)]
        public string FiscalName { get; set; } = string.Empty;

        [MaxLength(15)]
        public string Dni { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Cif { get; set; } = string.Empty;

        [MaxLength(200)]
        public string BillingStreet { get; set; } = string.Empty;

        [MaxLength(120)]
        public string BillingCity { get; set; } = string.Empty;

        [MaxLength(120)]
        public string BillingProvince { get; set; } = string.Empty;

        [MaxLength(20)]
        public string BillingPostalCode { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
        public bool EmailVerificado { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<ClienteDireccion> Direcciones { get; set; } = new List<ClienteDireccion>();
        public ICollection<ClienteMetodoPago> MetodosPago { get; set; } = new List<ClienteMetodoPago>();
    }
}
