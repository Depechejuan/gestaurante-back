using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;

namespace Gestaurante.Models.Services
{
    public class StaffService
    {
        private readonly AppDbContext _db;

        public StaffService(AppDbContext db)
        {
            _db = db;
        }

        public EmpleadoBasicDTO? GetBasicStaff(Guid id)
        {
            var empleado = _db.Empleados.FirstOrDefault(e => e.Id == id);
            if (empleado == null)
                return null;

            TipoEmpleado tipo;
            if (empleado is Administrador)
                tipo = TipoEmpleado.Administrador;
            else if (empleado is Camarero)
                tipo = TipoEmpleado.Camarero;
            else
                tipo = TipoEmpleado.Cocinero;

            return new EmpleadoBasicDTO(
                    empleado.Id,
                    tipo
                ); ;
        }
    }
}
