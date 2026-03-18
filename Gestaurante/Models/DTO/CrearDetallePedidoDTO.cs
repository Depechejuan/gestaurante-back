using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class CrearDetallePedidoDTO
    {
        [Required]
        public Guid IdPlato { get; set; }

        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; } = 1;
    }
}
