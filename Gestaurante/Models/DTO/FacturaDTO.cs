using Gestaurante.Models.Entities;

namespace Gestaurante.Models.DTO
{
    public class FacturaDTO
    {
        public Guid NumeroFactura { get; set; }
        public Guid? IdMesa { get; set; }
        public Guid? IdPedido { get; set; }
        public CanalPedido? CanalPedido { get; set; }
        public double PrecioTotal { get; set; }
        public double Descuento { get; set; }
        public double TotalConDescuento { get; set; }
        public EstadoFactura Estado { get; set; }
        public DateTime FechaFactura { get; set; }
        public List<Guid> PedidoIds { get; set; } = new();
    }
}
