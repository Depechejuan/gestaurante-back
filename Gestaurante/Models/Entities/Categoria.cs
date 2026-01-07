using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class Categoria
    {
        [Required]
        [MaxLength(100)]
        public Guid IdCategoria { get; protected set; }
        [Required]
        public string Descripcion { get; set; } = string.Empty;
        public Categoria() { }
        public Categoria(Guid idCategoria, string descripcion)
        {
            IdCategoria = idCategoria;
            Descripcion = descripcion;
        }
    }
}
