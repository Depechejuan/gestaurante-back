using Gestaurante.Models.Entities;

namespace Gestaurante.Models.DTO
{
    public class PedidoDTO
    {
        public Guid IdPedido { get; set; }
        public Guid? IdMesa { get; set; }
        public Guid? IdFactura { get; set; }
        public Guid? IdUsuarioCliente { get; set; }
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public EstadoPedido Estado { get; set; }
        public CanalPedido CanalPedido { get; set; }
        public TipoEntrega TipoEntrega { get; set; }
        public EstadoPago EstadoPago { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteEmail { get; set; } = string.Empty;
        public string ClienteTelefono { get; set; } = string.Empty;
        public string ClienteDireccionSnapshot { get; set; } = string.Empty;
        public string Notas { get; set; } = string.Empty;
        public double SubtotalProductos { get; set; }
        public double GastosEnvio { get; set; }
        public double Total { get; set; }
        public bool EstaFacturado { get; set; }
        public bool TieneLineasActivas { get; set; }
        public List<DetallePedidoDTO> Detalles { get; set; } = new();
    }
}
