namespace Gestaurante.Models.DTO
{
    public class CheckoutPaymentMethodDTO
    {
        public Guid? IdClienteMetodoPago { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string HolderName { get; set; } = string.Empty;
        public int? ExpMonth { get; set; }
        public int? ExpYear { get; set; }
        public bool SaveForFuture { get; set; }
    }
}
