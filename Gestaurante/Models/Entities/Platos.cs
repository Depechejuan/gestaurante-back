using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class Platos
    {
        [Required]
        [MaxLength(100)]
        public Guid IdPlato { get; protected set; }

        [Required]
        public string Nombre { get; set; } = string.Empty;
        [Required]
        public string Descripcion { get; set; } = string.Empty;

        public string Imagen { get; set; } = string.Empty;
        [Required]
        public bool Disponible { get; set; } = false;
        public List<Guid> IdIngredientes { get; set; }
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
        public decimal Precio { get; set; }
        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; protected set; }
        protected Platos() { }
        public Platos(Guid idPlato, string nombre, string descripcion, string imagen, bool disponibilidad, List<Guid> listadoIngredientes
            , decimal precio)
        {
            if (precio < 0)
            {
                throw new ArgumentException("El precio no puede ser negativo", nameof(precio));
            }
            IdPlato = idPlato;
            Nombre = nombre;
            Descripcion = descripcion;
            Imagen = imagen;
            Disponible = disponibilidad;
            IdIngredientes = listadoIngredientes;
            Precio = precio;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
