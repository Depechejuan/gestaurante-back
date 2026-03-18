using Gestaurante.Models.Entities;

namespace Gestaurante.Models.DTO
{
    public class PedidoDTO
    {
        public Guid IdPedido { get; set; }
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public EstadoPedido Estado { get; set; }
        public double Total { get; set; }
        public List<DetallePedidoDTO> Detalles { get; set; } = new();
    }
}
