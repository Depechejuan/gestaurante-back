namespace Gestaurante.Models.DTO
{
    public class EmpleadoBasicDTO
    {
        public Guid Id { get; set; }
        public TipoEmpleado Tipo { get; set; }

        public EmpleadoBasicDTO(Guid id, TipoEmpleado tipo)
        {
            this.Id = id;
            this.Tipo = tipo;
        }
    }
}
