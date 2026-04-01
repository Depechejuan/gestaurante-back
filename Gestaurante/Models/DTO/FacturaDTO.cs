using Gestaurante.Models.Entities;

namespace Gestaurante.Models.DTO
{
    public class FacturaDTO
    {
        public Guid NumeroFactura { get; set; }
        public Guid? IdMesa { get; set; }
        public Guid? IdPedido { get; set; }
        public Guid? IdUsuarioCliente { get; set; }
        public CanalPedido? CanalPedido { get; set; }
        public double PrecioTotal { get; set; }
        public double Descuento { get; set; }
        public TipoDescuentoFactura TipoDescuento { get; set; }
        public double ValorDescuento { get; set; }
        public string MotivoDescuento { get; set; } = string.Empty;
        public double TotalConDescuento { get; set; }
        public EstadoFactura Estado { get; set; }
        public DateTime FechaFactura { get; set; }
        public MetodoPagoFactura? MetodoCobro { get; set; }
        public double? ImporteEntregado { get; set; }
        public double? CambioEntregado { get; set; }
        public DateTime? FechaCobro { get; set; }
        public FacturaClienteDTO ClienteFactura { get; set; } = new();
        public List<FacturaLineaDTO> Lineas { get; set; } = new();
        public List<Guid> PedidoIds { get; set; } = new();
    }
}
