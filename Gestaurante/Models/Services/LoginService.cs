using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

namespace Gestaurante.Models.Services
{
    public class LoginService
    {
        private readonly AppDbContext _db;

        public LoginService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<EmpleadoLoginDTO?> Login(LoginDTO dto)
        {
            var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Email.ToLower() == dto.Email.ToLower());

            if (empleado == null || !empleado.Activo)
                return null;

            TipoEmpleado tipo;
            if (empleado is Administrador)
                tipo = TipoEmpleado.Administrador;
            else if (empleado is Camarero)
                tipo = TipoEmpleado.Camarero;
            else
                tipo = TipoEmpleado.Cocinero;

            if (ValidarCredenciales(empleado.Password, dto.Password)) {
                return new EmpleadoLoginDTO(
                    empleado.Id,
                    empleado.Email,
                    tipo
                );
            }
            return null;
        }

        public bool ValidarCredenciales(string passwordHashed, string password)
        {
            if (string.IsNullOrEmpty(passwordHashed) || string.IsNullOrEmpty(password))
                return false;

            // Verificar la contraseña
            if (BCrypt.Net.BCrypt.Verify(password, passwordHashed))
                return true;
            return false;
        }
    }
}
