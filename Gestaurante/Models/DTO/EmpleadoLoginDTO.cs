namespace Gestaurante.Models.DTO
{
    public class EmpleadoLoginDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public TipoEmpleado Tipo { get; set; }

        public EmpleadoLoginDTO(Guid id, string email, TipoEmpleado tipo)
        {
            this.Id = id;
            this.Email = email;
            this.Tipo = tipo;
        }
    }
}
