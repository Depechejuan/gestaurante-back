using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Gestaurante.Models.DTO
{
    public class UpsertPlatoDTO
    {
        public Guid IdPlato { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string? Imagen { get; set; }
        public bool Disponible { get; set; }
        public decimal Precio { get; set; }
        public Guid IdCategoria { get; set; }
        public string CategoriaDescripcion { get; set; } = string.Empty;
        public string IngredientesJson { get; set; } = "[]";
        public IFormFile? Photo { get; set; }

        public PlatoDTO ToPlatoDto()
        {
            return new PlatoDTO
            {
                IdPlato = IdPlato,
                Nombre = Nombre,
                Descripcion = Descripcion,
                Imagen = Imagen?.Trim() ?? string.Empty,
                Disponible = Disponible,
                Precio = Precio,
                IdCategoria = IdCategoria,
                CategoriaDescripcion = CategoriaDescripcion?.Trim() ?? string.Empty,
                Ingredientes = ParseIngredientes()
            };
        }

        private List<PlatoIngredienteDTO> ParseIngredientes()
        {
            if (string.IsNullOrWhiteSpace(IngredientesJson))
                return new List<PlatoIngredienteDTO>();

            try
            {
                return JsonSerializer.Deserialize<List<PlatoIngredienteDTO>>(IngredientesJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<PlatoIngredienteDTO>();
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("El formato de ingredientes no es válido.");
            }
        }
    }
}
