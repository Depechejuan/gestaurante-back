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
        [Key]
        [Required]
        [MaxLength(100)]
        public Guid NumeroFactura { get; private set; }

        public Guid? IdPedido { get; set; }
        [Required]
        public double PrecioTotal { get; set; }
        public double Descuento { get; set; }
        public EstadoFactura Estado { get; set; } = EstadoFactura.PENDIENTE;
        public DateTime FechaFactura { get; set; } = DateTime.UtcNow;
        public Factura() { }
        public Factura(Guid numeroFactura, Guid? idPedido, double precioTotal, double descuento, EstadoFactura estado, DateTime? fechaFactura = null) 
        {
            NumeroFactura = numeroFactura;
            IdPedido = idPedido;
            PrecioTotal = precioTotal;
            Descuento = descuento;
            Estado = estado;
            FechaFactura = fechaFactura ?? DateTime.UtcNow;
        }
    }
}
