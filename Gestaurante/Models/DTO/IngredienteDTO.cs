using System.Text.Json.Serialization;

namespace Gestaurante.Models.DTO
{
    public class IngredienteDTO
    {
        public Guid IdIngrediente { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Alergenico { get; set; } = false;
        public bool Disponible { get; set; } = false;
        public string Imagen { get; set; } = string.Empty;
        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [JsonConstructor]
        public IngredienteDTO(string Nombre, bool Alergenico, bool Disponible, string Imagen)
        {
            IdIngrediente = new Guid();
            this.Nombre = Nombre;
            this.Alergenico = Alergenico;
            this.Disponible = Disponible;
            this.Imagen = Imagen;
        }
        public IngredienteDTO(Guid id, string nombre, bool alergenico, bool disponible, string imagen)
        {
            IdIngrediente = id;
            Nombre = nombre;
            Alergenico = alergenico;
            Disponible = disponible;
            Imagen = imagen;
        }
    }
}
