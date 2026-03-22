using System;
using System.Collections.Generic;

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
        public Guid IdCategoria { get; set; } // Solo el ID de la categoría
        public ICollection<PlatoIngredienteDTO> Ingredientes { get; set; } = new List<PlatoIngredienteDTO>();

        public PlatoDTO() { }

        public PlatoDTO(string nombre, string descripcion, string imagen, bool disponible, decimal precio, Guid idCategoria, ICollection<PlatoIngredienteDTO> ingredientes)
        {
            IdPlato = Guid.NewGuid();
            Nombre = nombre;
            Descripcion = descripcion;
            Imagen = imagen;
            Disponible = disponible;
            Precio = precio;
            IdCategoria = idCategoria;
            Ingredientes = ingredientes;
        }
    }
}
