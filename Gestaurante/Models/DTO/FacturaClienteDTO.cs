namespace Gestaurante.Models.DTO
{
    public class FacturaClienteDTO
    {
        public Guid? IdUsuarioCliente { get; set; }
        public string BillingName { get; set; } = string.Empty;
        public string BillingDocument { get; set; } = string.Empty;
        public string BillingStreet { get; set; } = string.Empty;
        public string BillingCity { get; set; } = string.Empty;
        public string BillingProvince { get; set; } = string.Empty;
        public string BillingPostalCode { get; set; } = string.Empty;
        public string BillingEmail { get; set; } = string.Empty;
        public string BillingPhone { get; set; } = string.Empty;
        public bool EsAnonima { get; set; }
    }
}
