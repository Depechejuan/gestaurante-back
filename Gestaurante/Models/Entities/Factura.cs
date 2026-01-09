using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class Factura
    {
        [Required]
        [MaxLength(100)]
        public Guid NumeroFactura { get; protected set; }
        [Required]
        public Guid IdPlato { get; }
        [Required]
        public double PrecioTotal { get; set; }
        public double Descuento { get; set; }
        public bool Estado { get; set; }
        public Factura() { }
        public Factura(Guid numeroFactura, Guid idPlato, double precioTotal, double descuento, bool estado) 
        {
            NumeroFactura = numeroFactura;
            IdPlato = idPlato;
            PrecioTotal = precioTotal;
            Descuento = descuento;
            Estado = estado;
        }
    }
}
