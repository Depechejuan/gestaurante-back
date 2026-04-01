using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class CobrarFacturaDTO
    {
        public MetodoPagoFactura MetodoPago { get; set; }

        [Range(0, double.MaxValue)]
        public double? ImporteEntregado { get; set; }
    }
}
