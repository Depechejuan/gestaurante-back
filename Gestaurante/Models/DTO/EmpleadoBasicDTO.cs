namespace Gestaurante.Models.DTO
{
    public class EmpleadoBasicDTO
    {
        public Guid Id { get; set; }
        public TipoEmpleado Tipo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido1 { get; set; } = string.Empty;
        public string Apellido2 { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public EmpleadoBasicDTO(Guid id, TipoEmpleado tipo, string nombre, string apellido1, string apellido2, string email)
        {
            Id = id;
            Tipo = tipo;
            Nombre = nombre;
            Apellido1 = apellido1;
            Apellido2 = apellido2;
            Email = email;
        }
    }
}
