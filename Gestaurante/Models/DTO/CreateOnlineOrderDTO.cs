using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class CreateOnlineOrderDTO
    {
        [Required]
        public TipoEntrega TipoEntrega { get; set; }

        [Required]
        public bool PagarOnline { get; set; }

        public Guid? IdClienteDireccion { get; set; }
        public string Notas { get; set; } = string.Empty;

        [Required]
        public List<CrearDetallePedidoDTO> Detalles { get; set; } = new();

        public CheckoutPaymentMethodDTO? PaymentMethod { get; set; }
    }
}
