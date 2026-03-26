using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class CrearPedidoPublicoDTO
    {
        [Required]
        public List<CrearDetallePedidoDTO> Detalles { get; set; } = new();
    }
}
