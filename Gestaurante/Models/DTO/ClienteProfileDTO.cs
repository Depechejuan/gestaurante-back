namespace Gestaurante.Models.DTO
{
    public class ClienteProfileDTO
    {
        public Guid IdUsuarioCliente { get; set; }
        public string Email { get; set; } = string.Empty;
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
        public bool Activo { get; set; }
        public bool EmailVerificado { get; set; }
    }
}
