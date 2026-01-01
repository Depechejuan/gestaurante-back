using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Gestaurante.Models.Services
{
    public class RegisterService
    {
        public Empleado CrearEmpleado(RegistroDTO dto)
        {
            if (dto.Tipo == TipoEmpleado.Administrador)
                return new Administrador(dto.Id, dto.Email, dto.Password, dto.FirstName,
                    dto.FirstLastName, dto.SecondLastName, dto.DNI, dto.NUSS, dto.CreatedAt);
            if (dto.Tipo == TipoEmpleado.Camarero)
                return new Camarero(dto.Id, dto.Email, dto.Password, dto.FirstName,
                    dto.FirstLastName, dto.SecondLastName, dto.DNI, dto.NUSS, dto.CreatedAt);
            if (dto.Tipo == TipoEmpleado.Cocinero)
                return new Cocinero(dto.Id, dto.Email, dto.Password, dto.FirstName,
                    dto.FirstLastName, dto.SecondLastName, dto.DNI, dto.NUSS, dto.CreatedAt);

            throw new Exception("El tipo de empleado no es válido");
        }
    }
}
