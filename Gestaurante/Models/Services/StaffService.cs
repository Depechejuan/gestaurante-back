using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Models.Services
{
    public class StaffService
    {
        private readonly AppDbContext _db;

        public StaffService(AppDbContext db)
        {
            _db = db;
        }

        public List<EmpleadoFullDTO> GetAllUsers()
        {
            var empleados = _db.Empleados.ToList();
            List<EmpleadoFullDTO> empleadosDto = new();

            foreach (var empleado in empleados)
            {
                TipoEmpleado tipo;

                if (empleado is Administrador)
                    tipo = TipoEmpleado.Administrador;
                else if (empleado is Camarero)
                    tipo = TipoEmpleado.Camarero;
                else
                    tipo = TipoEmpleado.Cocinero;

                var dto = new EmpleadoFullDTO(
                    empleado.Id,
                    empleado.FirstName,
                    empleado.FirstLastName,
                    empleado.SecondLastName,
                    empleado.Email,
                    empleado.DNI,
                    empleado.NUSS,
                    tipo
                );

                empleadosDto.Add(dto);
            }

            return empleadosDto;
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
