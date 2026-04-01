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
        public Guid? IdUsuarioCliente { get; set; }
        [Required]
        public double PrecioTotal { get; set; }
        public double Descuento { get; set; }
        public TipoDescuentoFactura TipoDescuento { get; set; } = TipoDescuentoFactura.FIJO;
        public double ValorDescuento { get; set; }
        [MaxLength(250)]
        public string MotivoDescuento { get; set; } = string.Empty;
        public EstadoFactura Estado { get; set; } = EstadoFactura.PENDIENTE;
        public CanalPedido? CanalPedido { get; set; }
        public DateTime FechaFactura { get; set; } = DateTime.UtcNow;
        public MetodoPagoFactura? MetodoCobro { get; set; }
        public double? ImporteEntregado { get; set; }
        public double? CambioEntregado { get; set; }
        public DateTime? FechaCobro { get; set; }

        [Required]
        [MaxLength(160)]
        public string BillingName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string BillingDocument { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string BillingStreet { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string BillingCity { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string BillingProvince { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string BillingPostalCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string BillingEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(25)]
        public string BillingPhone { get; set; } = string.Empty;

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
