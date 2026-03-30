namespace Gestaurante.Models.DTO
{
    public class AsignarFacturaClienteDTO
    {
        public Guid? IdUsuarioCliente { get; set; }
        public string FiscalName { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Cif { get; set; } = string.Empty;
        public string BillingStreet { get; set; } = string.Empty;
        public string BillingCity { get; set; } = string.Empty;
        public string BillingProvince { get; set; } = string.Empty;
        public string BillingPostalCode { get; set; } = string.Empty;
        public string BillingEmail { get; set; } = string.Empty;
        public string BillingPhone { get; set; } = string.Empty;
        public bool SaveOnCustomer { get; set; } = true;
    }
}
