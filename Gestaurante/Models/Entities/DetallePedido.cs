using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class DetallePedido
    {
        [Key]
        [Required]
        [MaxLength(100)]
        public Guid IdDetallePedido { get; private set; }
        [Required]
        public Guid IdPlato { get; set; }
        [Required]
        public Guid IdPedido { get; set; }
        [Required]
        public int Cantidad { get; set; } = 1;
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
        public double PrecioUnitario { get; set; }
        public DetallePedido() { }
        public DetallePedido(Guid idDetallePedido, Guid idPlato, Guid idPedido, int cantidad, double precioUnitario)
        {
            IdDetallePedido = idDetallePedido;
            IdPlato = idPlato;
            IdPedido = idPedido;
            Cantidad = cantidad;
            PrecioUnitario = precioUnitario;
        }


    }
}
