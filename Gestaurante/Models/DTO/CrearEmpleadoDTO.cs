using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Models.DTO
{
    public enum TipoEmpleado
    {
        Camarero,
        Cocinero,
        Administrador
    }

    public class CrearEmpleadoDTO
    {
        [Required]
        public TipoEmpleado Tipo { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string FirstLastName { get; set; } = string.Empty;
        [Required]
        public string SecondLastName { get; set; } = string.Empty;

        public string DNI { get; set; } = string.Empty;
        public string NUSS { get; set; } = string.Empty;
    }
}
