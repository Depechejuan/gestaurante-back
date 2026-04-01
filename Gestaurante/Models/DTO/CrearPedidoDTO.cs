using Gestaurante.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class CrearPedidoDTO
    {
        public Guid? IdMesa { get; set; }

        public EstadoPedido Estado { get; set; } = EstadoPedido.PENDIENTE;
        public CanalPedido CanalPedido { get; set; } = CanalPedido.SALA;
        public TipoEntrega TipoEntrega { get; set; } = TipoEntrega.MESA;
        public EstadoPago EstadoPago { get; set; } = EstadoPago.NO_APLICA;
        public Guid? IdUsuarioCliente { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteEmail { get; set; } = string.Empty;
        public string ClienteTelefono { get; set; } = string.Empty;
        public string ClienteDireccionSnapshot { get; set; } = string.Empty;
        public double GastosEnvio { get; set; } = 0;
        public string Notas { get; set; } = string.Empty;
        public List<CrearDetallePedidoDTO> Detalles { get; set; } = new();
    }
}
