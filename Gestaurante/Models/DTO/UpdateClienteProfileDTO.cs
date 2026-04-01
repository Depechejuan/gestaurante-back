namespace Gestaurante.Models.DTO
{
    public class UpdateClienteProfileDTO
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string FiscalName { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Cif { get; set; } = string.Empty;
        public string BillingStreet { get; set; } = string.Empty;
        public string BillingCity { get; set; } = string.Empty;
        public string BillingProvince { get; set; } = string.Empty;
        public string BillingPostalCode { get; set; } = string.Empty;
    }
}
