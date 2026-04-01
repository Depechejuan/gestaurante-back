namespace Gestaurante.Models.DTO
{
    public class ClienteDireccionDTO
    {
        public Guid IdClienteDireccion { get; set; }
        public string Alias { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
