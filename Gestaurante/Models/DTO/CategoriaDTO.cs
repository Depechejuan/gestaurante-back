using System.Text.Json.Serialization;

namespace Gestaurante.Models.DTO
{
    public class CategoriaDTO
    {
        Guid IdCategoria { get; set; }
        public string Descripcion { get; set; } = string.Empty;

        public CategoriaDTO(string Descripcion)
        {
            IdCategoria = new Guid();
            this.Descripcion = Descripcion;
        }
        public CategoriaDTO(Guid IdCategoria, string Descripcion)
        {
            this.IdCategoria = IdCategoria;
            this.Descripcion = Descripcion;
        }
    }
}
