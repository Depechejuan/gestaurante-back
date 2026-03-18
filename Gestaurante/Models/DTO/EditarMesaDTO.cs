using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public class EditarMesaDTO
    {
        [Range(1, int.MaxValue)]
        public int? Capacidad { get; set; }

        public bool? Estado { get; set; }

        [MaxLength(100)]
        public string? Ubicacion { get; set; }
    }
}
