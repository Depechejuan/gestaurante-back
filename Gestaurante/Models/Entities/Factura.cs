using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class Factura
    {
        [Key]
        [Required]
        [MaxLength(100)]
        public Guid NumeroFactura { get; private set; }

        public Guid? IdMesa { get; set; }
        public Guid? IdPedido { get; set; }
        [Required]
        public double PrecioTotal { get; set; }
        public double Descuento { get; set; }
        public EstadoFactura Estado { get; set; } = EstadoFactura.PENDIENTE;
        public CanalPedido? CanalPedido { get; set; }
        public DateTime FechaFactura { get; set; } = DateTime.UtcNow;
        public Factura() { }
        public Factura(Guid numeroFactura, Guid? idMesa, Guid? idPedido, double precioTotal, double descuento, EstadoFactura estado, DateTime? fechaFactura = null, CanalPedido? canalPedido = null) 
        {
            NumeroFactura = numeroFactura;
            IdMesa = idMesa;
            IdPedido = idPedido;
            PrecioTotal = precioTotal;
            Descuento = descuento;
            Estado = estado;
            FechaFactura = fechaFactura ?? DateTime.UtcNow;
            CanalPedido = canalPedido;
        }
    }
}
