using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class Ingrediente
    {
        [Key]
        [Required]
        [MaxLength(100)]
        public Guid IdIngrediente { get; private set; }
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty; 
        [Required]
        public bool Alergenico { get; set; } = false;
        [Required]
        public bool Disponible { get; set; } = false;
        public string Imagen { get; set; } = string.Empty;
        public virtual ICollection<PlatoIngrediente> PlatoIngredientes { get; set; } = new List<PlatoIngrediente>();
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        protected Ingrediente() {}
        public Ingrediente(Guid idIngrediente, string nombre,  bool alergenico, bool disponibilidad, string imagen)
        {
            IdIngrediente = idIngrediente;
            Nombre = nombre;
            Alergenico = alergenico;
            Disponible = disponibilidad;
            Imagen = imagen;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
