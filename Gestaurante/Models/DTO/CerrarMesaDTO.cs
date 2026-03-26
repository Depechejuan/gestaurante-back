using Gestaurante.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class CerrarMesaDTO
    {
        [Range(0, double.MaxValue)]
        public double Descuento { get; set; } = 0;

        public EstadoFactura EstadoFactura { get; set; } = EstadoFactura.PENDIENTE;
        public DateTime? FechaFactura { get; set; }
    }
}
