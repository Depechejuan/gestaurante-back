using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class CreateInternalClienteDTO
    {
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(160)]
        public string FiscalName { get; set; } = string.Empty;

        [MaxLength(120)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(160)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(25)]
        public string Phone { get; set; } = string.Empty;

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
    }
}
