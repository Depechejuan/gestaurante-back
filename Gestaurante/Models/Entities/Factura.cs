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
        public double PrecioTotal { get; set; }
        public double Descuento { get; set; }
        public EstadoFactura Estado { get; set; } = EstadoFactura.PENDIENTE;
        public DateTime FechaFactura { get; set; } = DateTime.UtcNow;
        public Factura() { }
        public Factura(Guid numeroFactura, Guid idPlato, double precioTotal, double descuento, EstadoFactura estado) 
        {
            NumeroFactura = numeroFactura;
            PrecioTotal = precioTotal;
            Descuento = descuento;
            Estado = estado;
        }
    }
}
