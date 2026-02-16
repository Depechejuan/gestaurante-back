using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class StaffService
    {
        private readonly AppDbContext _db;

        public StaffService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<EmpleadoFullDTO>> GetAllUsers()
        {
            var empleados = await _db.Empleados.ToListAsync();
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


        public async Task<EmpleadoBasicDTO?> GetBasicStaff(Guid id)
        {
            var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == id);
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
                );
        }

        public async Task<EmpleadoFullDTO> GetFullUser(Guid id)
        {
            var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == id);
            TipoEmpleado tipo;
            if (empleado is Administrador)
                tipo = TipoEmpleado.Administrador;
            else if (empleado is Camarero)
                tipo = TipoEmpleado.Camarero;
            else
                tipo = TipoEmpleado.Cocinero;
            Console.WriteLine(empleado);

            return new EmpleadoFullDTO(empleado.Id, empleado.FirstName, empleado.FirstLastName, empleado.SecondLastName, empleado.Email, empleado.DNI, empleado.NUSS, tipo);
        }
    }
}
