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
        [Required]
        [MaxLength(100)]
        public Guid IdPedido { get; protected set; }
        
        //relacion con tabla DetallePedido
        public virtual ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();

        [Required]
        public DateTime FechaPedido { get; protected set; } = DateTime.UtcNow;
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

        //metodo para agregar platos al pedido
        public void AgregarPlato(Plato plato, int cantidad)
        {
            var detalle = new DetallePedido(Guid.NewGuid(),plato.IdPlato,this.IdPedido,cantidad,(double)plato.Precio);

            DetallesPedido.Add(detalle);
        }
    }
}
