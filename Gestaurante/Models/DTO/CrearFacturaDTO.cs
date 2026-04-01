using Gestaurante.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class CrearFacturaDTO
    {
        public Guid? IdMesa { get; set; }
        public Guid? IdPedido { get; set; }
        public CanalPedido? CanalPedido { get; set; }

        [Range(0, double.MaxValue)]
        public double? PrecioTotal { get; set; }

        [Range(0, double.MaxValue)]
        public double Descuento { get; set; } = 0;
        [MaxLength(250)]
        public string MotivoDescuento { get; set; } = string.Empty;

        public EstadoFactura Estado { get; set; } = EstadoFactura.PENDIENTE;
        public DateTime? FechaFactura { get; set; }
    }
}
