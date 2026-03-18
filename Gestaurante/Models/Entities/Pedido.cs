using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public enum EstadoPedido
    {
        PENDIENTE,
        CONFIRMADO,
        PREPARACION,
        LISTO,
        ENTREGADO,
        CANCELADO
    }
    public class Pedido
    {
        [Key]
        [Required]
        [MaxLength(100)]
        public Guid IdPedido { get; private set; }
        
        //relacion con tabla DetallePedido
        public virtual ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();

        [Required]
        public DateTime FechaPedido { get; set; } = DateTime.UtcNow;
        public DateTime? FechaModificacion { get; protected set; }

        [Required]
        public EstadoPedido Estado { get; set; } = EstadoPedido.PENDIENTE;

        public Pedido() { }

        public Pedido(Guid idPedido, DateTime fechaPedido, EstadoPedido estado) 
        {
            IdPedido = idPedido;
            FechaPedido = fechaPedido;
            Estado = estado;
        }
    }
}
