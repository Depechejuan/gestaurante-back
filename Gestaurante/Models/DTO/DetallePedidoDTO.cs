using Gestaurante.Models.Entities;

namespace Gestaurante.Models.DTO
{
    public class DetallePedidoDTO
    {
        public Guid IdDetallePedido { get; set; }
        public Guid IdPedido { get; set; }
        public Guid IdPlato { get; set; }
        public string PlatoNombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public double PrecioUnitario { get; set; }
        public double Subtotal { get; set; }
        public EstadoDetallePedido Estado { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public bool SeTieneEnCuentaEnFactura { get; set; }
    }
}
