using Microsoft.AspNetCore.Components.Web;
using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.Entities
{
    public class Mesa
    {
        [Key]
        [Required]
        [MaxLength(100)]
        public Guid IdMesa { get; private set; }
        public int Capacidad { get; set; }
        public bool Estado { get; set; }
        [Required]
        public string Ubicacion { get; set; } = string.Empty;
        public Mesa() { }
        public Mesa(Guid idMesa, int capacidad, bool estado, string ubicacion)
        { 
            IdMesa = idMesa;
            Capacidad = capacidad;
            Estado = estado;
            Ubicacion = ubicacion;
        }
    }
}
