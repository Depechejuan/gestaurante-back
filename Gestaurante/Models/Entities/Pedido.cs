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

        public virtual ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();

        [Required]
        public DateTime FechaPedido { get; set; } = DateTime.UtcNow;
        public DateTime? FechaModificacion { get; protected set; }

        [Required]
        public EstadoPedido Estado { get; set; } = EstadoPedido.PENDIENTE;

        public Pedido() { }

        public Pedido(Guid idPedido, Guid? idMesa, DateTime fechaPedido, EstadoPedido estado, Guid? idMesaPublicSession = null)
        {
            IdPedido = idPedido;
            IdMesa = idMesa;
            FechaPedido = fechaPedido;
            Estado = estado;
            IdMesaPublicSession = idMesaPublicSession;
        }
    }
}
