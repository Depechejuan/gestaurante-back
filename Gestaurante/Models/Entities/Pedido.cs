using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class Pedido
    {
        [Required]
        [MaxLength(100)]
        public Guid IdPedido { get; protected set; }
        [Required]
        public DateTime FechaPedido { get; protected set; } = DateTime.UtcNow;
        [Required]
        public string Estado { get; set; } = string.Empty;
        public Pedido() { }

        public Pedido(Guid idPedido, DateTime fechaPedido, string estado) 
        {
            IdPedido = idPedido;
            FechaPedido = fechaPedido;
            Estado = estado;
        }
    }
}
