namespace Gestaurante.Models.DTO
{
    public class IngredienteDTO
    {
        public Guid IdIngrediente { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Alergenico { get; set; }
        public bool Disponible { get; set; }
        public string Imagen { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
