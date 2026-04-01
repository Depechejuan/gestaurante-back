namespace Gestaurante.Models.DTO
{
    public class FacturaClienteLookupDTO
    {
        public Guid IdUsuarioCliente { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FiscalName { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Cif { get; set; } = string.Empty;
        public string BillingStreet { get; set; } = string.Empty;
        public string BillingCity { get; set; } = string.Empty;
        public string BillingProvince { get; set; } = string.Empty;
        public string BillingPostalCode { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool EsAnonimo { get; set; }
    }
}
