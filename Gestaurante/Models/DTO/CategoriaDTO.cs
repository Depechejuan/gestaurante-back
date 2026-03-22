using System;

namespace Gestaurante.Models.DTO
{
    public class CategoriaDTO
    {
        public Guid IdCategoria { get; set; }
        public string Descripcion { get; set; } = string.Empty;

        public CategoriaDTO() { }

        public CategoriaDTO(Guid idCategoria, string descripcion)
        {
            IdCategoria = idCategoria;
            Descripcion = descripcion;
        }
    }
}
