using Gestaurante.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class CrearFacturaDTO
    {
        public Guid? IdPedido { get; set; }

        [Range(0, double.MaxValue)]
        public double? PrecioTotal { get; set; }

        [Range(0, double.MaxValue)]
        public double Descuento { get; set; } = 0;

        public EstadoFactura Estado { get; set; } = EstadoFactura.PENDIENTE;
        public DateTime? FechaFactura { get; set; }
    }
}
