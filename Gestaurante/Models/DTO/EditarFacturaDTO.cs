using Gestaurante.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class EditarFacturaDTO
    {
        public Guid? IdMesa { get; set; }
        public Guid? IdPedido { get; set; }

        [Range(0, double.MaxValue)]
        public double? PrecioTotal { get; set; }

        [Range(0, double.MaxValue)]
        public double? Descuento { get; set; }

        public EstadoFactura? Estado { get; set; }
        public DateTime? FechaFactura { get; set; }
    }
}
