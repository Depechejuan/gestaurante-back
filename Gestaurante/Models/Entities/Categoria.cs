using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class Categoria
    {
        [Key]
        [Required]
        [MaxLength(100)]
        public Guid IdCategoria { get; private set; }
        [Required]
        public string Descripcion { get; set; } = string.Empty;
        public virtual ICollection<Plato> Platos { get; set; } = new List<Plato>();
        public Categoria() { }
        public Categoria(Guid idCategoria, string descripcion)
        {
            IdCategoria = idCategoria;
            Descripcion = descripcion;
        }
    }
}
