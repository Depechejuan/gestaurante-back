using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class Plato
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
        public virtual ICollection<PlatoIngrediente> PlatoIngredientes { get; set; } = new List<PlatoIngrediente>();
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
        public decimal Precio { get; set; }
        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; protected set; }

        [Required]
        public Guid IdCategoria { get; set; } // Clave foránea

        public virtual Categoria Categoria { get; set; } = null!; // Propiedad de navegación

        public virtual ICollection<Plato> Platos { get; set; } = new List<Plato>();

        public Plato() { }
        public Plato(Guid idPlato, string nombre, string descripcion, string imagen, bool disponibilidad, decimal precio)
        {
            IdPlato = idPlato;
            Nombre = nombre;
            Descripcion = descripcion;
            Imagen = imagen;
            Disponible = disponibilidad;
            Precio = precio;

            CreatedAt = DateTime.UtcNow;
        }
    }
}

