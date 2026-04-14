namespace Gestaurante.Models.DTO
{
    public class PlatoDTO
    {
        public Guid IdPlato { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Imagen { get; set; } = string.Empty;
        public bool Disponible { get; set; }
        public decimal Precio { get; set; }
        public Guid IdCategoria { get; set; }
        public string CategoriaDescripcion { get; set; } = string.Empty;
        public List<PlatoIngredienteDTO> Ingredientes { get; set; } = new();
        public List<string> Alergenos { get; set; } = new();
    }
}
