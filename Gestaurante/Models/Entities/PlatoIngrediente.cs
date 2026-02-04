namespace Gestaurante.Models.Entities
{
    public class PlatoIngrediente
    {
        [Required]
        [ForeignKey("Plato")]
        public Guid IdPlato { get; set; }

        [Required]
        [ForeignKey("Ingrediente")]
        public Guid IdIngrediente { get; set; }

            // Propiedades de navegación
        public virtual Plato Plato { get; set; } = null!;
        public virtual Ingrediente Ingrediente { get; set; } = null!;

        public PlatoIngrediente() { }

        public PlatoIngrediente(Guid platoId, Guid ingredienteId)
        {
            IdPlato = platoId;
            IdIngrediente = ingredienteId;
        }
    }
}
