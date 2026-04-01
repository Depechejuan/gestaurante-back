using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class CreateClienteDireccionDTO
    {
        [Required]
        public string Alias { get; set; } = string.Empty;
        [Required]
        public string Street { get; set; } = string.Empty;
        [Required]
        public string City { get; set; } = string.Empty;
        [Required]
        public string Province { get; set; } = string.Empty;
        [Required]
        public string PostalCode { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
