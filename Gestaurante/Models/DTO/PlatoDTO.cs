using Gestaurante.Models.Entities;
using System.Text.Json.Serialization;

namespace Gestaurante.Models.DTO
{
    public class PlatoDTO
    {
        public Guid IdPlato { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Imagen { get; set; } = string.Empty;
        public bool Disponible { get; set; } = false;
        public decimal Precio { get; set; }
        public Categoria Categoria { get; set; } = null!;
        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<PlatoIngrediente> Ingredientes { get; set; }


        [JsonConstructor]
        public PlatoDTO(string Nombre, string Descripcion, string Imagen, bool Disponible, decimal Precio, string Categoria, ICollection<PlatoIngrediente> ingredientes)
        {
            this.IdPlato = new Guid();
            this.Nombre = Nombre;
            this.Descripcion = Descripcion;
            this.Imagen = Imagen;
            this.Disponible = Disponible;
            this.Precio = Precio;
            Categoria newCat = new Categoria(Categoria);
            this.Categoria = newCat;
            this.Ingredientes = ingredientes;
        }
        public PlatoDTO(Guid PlatoId, string Nombre, string Descripcion, string Imagen, bool Disponible, decimal Precio, Categoria Categoria, ICollection<PlatoIngrediente> ingredientes) 
        {
            this.IdPlato = PlatoId;
            this.Nombre = Nombre;
            this.Descripcion = Descripcion;
            this.Imagen = Imagen;
            this.Disponible = Disponible;
            this.Precio = Precio;
            this.Categoria = Categoria;
            this.Ingredientes = ingredientes;
        }
    }
}
