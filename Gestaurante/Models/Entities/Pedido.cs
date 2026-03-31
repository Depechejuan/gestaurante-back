using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class Pedido
    {
        [Key]
        [Required]
        [MaxLength(100)]
        public Guid IdPedido { get; private set; }

        public Guid? IdMesa { get; set; }
        public Guid? IdFactura { get; set; }
        public Guid? IdMesaPublicSession { get; set; }
        public Guid? IdUsuarioCliente { get; set; }

        public virtual ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();

        [Required]
        public DateTime FechaPedido { get; set; } = DateTime.UtcNow;
        public DateTime? FechaModificacion { get; protected set; }

        [Required]
        public EstadoPedido Estado { get; set; } = EstadoPedido.PENDIENTE;
        [Required]
        public CanalPedido CanalPedido { get; set; } = CanalPedido.SALA;
        [Required]
        public TipoEntrega TipoEntrega { get; set; } = TipoEntrega.MESA;
        [Required]
        public EstadoPago EstadoPago { get; set; } = EstadoPago.NO_APLICA;
        [MaxLength(160)]
        public string ClienteNombre { get; set; } = string.Empty;
        [MaxLength(100)]
        public string ClienteEmail { get; set; } = string.Empty;
        [MaxLength(25)]
        public string ClienteTelefono { get; set; } = string.Empty;
        [MaxLength(400)]
        public string ClienteDireccionSnapshot { get; set; } = string.Empty;
        [Required]
        public double GastosEnvio { get; set; } = 0;
        [MaxLength(500)]
        public string Notas { get; set; } = string.Empty;

        public Pedido() { }

        public Pedido(
            Guid idPedido,
            Guid? idMesa,
            DateTime fechaPedido,
            EstadoPedido estado,
            Guid? idMesaPublicSession = null,
            Guid? idUsuarioCliente = null,
            CanalPedido canalPedido = CanalPedido.SALA,
            TipoEntrega tipoEntrega = TipoEntrega.MESA,
            EstadoPago estadoPago = EstadoPago.NO_APLICA)
        {
            IdPedido = idPedido;
            IdMesa = idMesa;
            FechaPedido = fechaPedido;
            Estado = estado;
            IdMesaPublicSession = idMesaPublicSession;
            IdUsuarioCliente = idUsuarioCliente;
            CanalPedido = canalPedido;
            TipoEntrega = tipoEntrega;
            EstadoPago = estadoPago;
        }
    }
}
