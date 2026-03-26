using Gestaurante.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class CrearPedidoDTO
    {
        [Required]
        public Guid IdMesa { get; set; }

        public EstadoPedido Estado { get; set; } = EstadoPedido.PENDIENTE;
        public List<CrearDetallePedidoDTO> Detalles { get; set; } = new();
    }
}
