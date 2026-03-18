using Microsoft.AspNetCore.Http;

namespace Gestaurante.Models.DTO
{
    public class EditarEmpleadoDTO
    {
        public string? Nombre { get; set; }
        public string? Apellido1 { get; set; }
        public string? Apellido2 { get; set; }
        public string? Email { get; set; }
        public string? DNI { get; set; }
        public string? NUSS { get; set; }
        public string? Password { get; set; }
        public TipoEmpleado? Tipo { get; set; }
        public IFormFile? Photo { get; set; }
    }
}
