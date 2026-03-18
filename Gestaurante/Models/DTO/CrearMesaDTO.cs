using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class CrearMesaDTO
    {
        [Range(1, int.MaxValue)]
        public int Capacidad { get; set; } = 4;

        public bool Estado { get; set; } = true;

        [Required]
        [MaxLength(100)]
        public string Ubicacion { get; set; } = string.Empty;
    }
}
