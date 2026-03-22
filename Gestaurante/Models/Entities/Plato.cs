using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class Plato
    {
        [Required]
        [MaxLength(100)]
        [Key]
        public Guid IdPlato { get; set; }

        [Required]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public string Descripcion { get; set; } = string.Empty;

        public string Imagen { get; set; } = string.Empty;

        [Required]
        public bool Disponible { get; set; } = false;

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
        public decimal Precio { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public Guid IdCategoria { get; set; }

        public virtual Categoria Categoria { get; set; } 

        public virtual ICollection<PlatoIngrediente> PlatoIngredientes { get; set; } = new List<PlatoIngrediente>();

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
