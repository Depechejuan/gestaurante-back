using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class Ingrediente
    {
        [Required]
        [MaxLength(100)]
        public Guid IdIngrediente { get; protected set; }

        [Required]
        public bool Alergenico { get; set; } = false;
        [Required]
        public bool Disponible { get; set; } = false;
        public string Imagen { get; set; } = string.Empty;

        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; protected set; }
        protected Ingrediente() { }
        public Ingrediente(Guid idIngrediente, bool alergenico, bool disponibilidad, string imagen)
        {
            IdIngrediente = idIngrediente;
            Alergenico = alergenico;
            Disponible = disponibilidad;
            Imagen = imagen;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
