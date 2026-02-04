using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public enum EstadoFactura
    {
        PENDIENTE,
        PAGADO,
        CANCELADO
    }
    public class Factura
    {
        [Required]
        [MaxLength(100)]
        public Guid NumeroFactura { get; protected set; }

        [Required]
        public decimal PrecioTotal { get; set; }
        public decimal Descuento { get; set; }
        public EstadoFactura Estado { get; set; } = EstadoFactura.PENDIENTE;
        public DateTime FechaFactura { get; set; } = DateTime.UtcNow;
        public Factura() { }
        public Factura(Guid numeroFactura, decimal precioTotal, decimal descuento, EstadoFactura estado) 
        {
            NumeroFactura = numeroFactura;
            PrecioTotal = precioTotal;
            Descuento = descuento;
            Estado = estado;
        }
    }
}
