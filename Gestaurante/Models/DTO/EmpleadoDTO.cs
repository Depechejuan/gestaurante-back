namespace Gestaurante.Models.DTO
{
    public class EmpleadoDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public TipoEmpleado Tipo { get; set; }

        public EmpleadoDTO(Guid id, string email, TipoEmpleado tipo)
        {
            this.Id = id;
            this.Email = email; 
            this.Tipo = tipo;
        }
    }
}
