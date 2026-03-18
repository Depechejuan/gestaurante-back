using Gestaurante.Models.Entities;

namespace Gestaurante.Models.DTO
{
    public class CrearPedidoDTO
    {
        public EstadoPedido Estado { get; set; } = EstadoPedido.PENDIENTE;
        public List<CrearDetallePedidoDTO> Detalles { get; set; } = new();
    }
}
