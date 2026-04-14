using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Utils;

namespace Gestaurante.Models.Services
{
    public class CatalogProjectionService
    {
        public PlatoDTO MapInternal(Plato plato)
        {
            var ingredientes = GetBaseIngredientes(plato);

            return BuildDto(
                plato,
                ingredientes,
                ingredientes.Select(ingrediente => ingrediente.Nombre));
        }

        public PlatoDTO MapPublic(Plato plato)
        {
            var ingredientesPublicos = PublicIngredientResolver.ResolveForPublic(GetBaseIngredientes(plato));

            return BuildDto(
                plato,
                ingredientesPublicos,
                ingredientesPublicos.Select(ingrediente => ingrediente.Nombre));
        }

        private static List<PlatoIngredienteDTO> GetBaseIngredientes(Plato plato)
        {
            return plato.PlatoIngredientes
                .OrderBy(pi => pi.Ingrediente != null ? pi.Ingrediente.Nombre : string.Empty)
                .Select(pi => new PlatoIngredienteDTO
                {
                    IdIngrediente = pi.IdIngrediente,
                    Nombre = pi.Ingrediente?.Nombre ?? string.Empty
                })
                .ToList();
        }

        private static PlatoDTO BuildDto(Plato plato, List<PlatoIngredienteDTO> ingredientes, IEnumerable<string> alergenosInput)
        {
            return new PlatoDTO
            {
                IdPlato = plato.IdPlato,
                Nombre = plato.Nombre,
                Descripcion = plato.Descripcion,
                Imagen = plato.Imagen,
                Disponible = plato.Disponible,
                Precio = plato.Precio,
                IdCategoria = plato.IdCategoria,
                CategoriaDescripcion = plato.Categoria?.Descripcion ?? string.Empty,
                Ingredientes = ingredientes,
                Alergenos = AllergenResolver.ResolveFromIngredientes(alergenosInput)
            };
        }
    }
}
