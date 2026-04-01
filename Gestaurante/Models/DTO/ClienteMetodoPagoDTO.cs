namespace Gestaurante.Models.DTO
{
    public class ClienteMetodoPagoDTO
    {
        public Guid IdClienteMetodoPago { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Last4 { get; set; } = string.Empty;
        public string HolderName { get; set; } = string.Empty;
        public int ExpMonth { get; set; }
        public int ExpYear { get; set; }
        public bool IsDefault { get; set; }
    }
}
