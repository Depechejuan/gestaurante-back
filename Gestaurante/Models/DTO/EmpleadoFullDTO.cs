namespace Gestaurante.Models.DTO
{
    public class EmpleadoFullDTO
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido1 { get; set; } = string.Empty;
        public string Apellido2 { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DNI { get; set; }
        public string NUSS { get; set; }
        public string Password { get; set; } = string.Empty;
        public TipoEmpleado Tipo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public EmpleadoFullDTO(Guid id, string nombre, string apellido1, string apellido2, string email, string dni, string nuss, TipoEmpleado tipo)
        {
            this.Id = id;
            this.Nombre = nombre;
            this.Apellido1 = apellido1;
            this.Apellido2 = apellido2;
            this.Email = email;
            this.DNI = dni;
            this.NUSS = nuss;
            this.Tipo = tipo;
        }
    }
}
