using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;

namespace Gestaurante.Models.Services
{
    public class RegisterService
    {
        public Empleado CrearEmpleado(RegistroDTO dto)
        {
            if (dto.Tipo == TipoEmpleado.Administrador)
                return new Administrador(dto);
            if (dto.Tipo == TipoEmpleado.Camarero)
                return new Camarero(dto);
            if (dto.Tipo == TipoEmpleado.Cocinero)
                return new Cocinero(dto);

            throw new Exception("El tipo de empleado no es válido");
        }
    }
}
