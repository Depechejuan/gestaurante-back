using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
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

        public EmpleadoLoginDTO? Login(LoginDTO dto)
        {
            var empleado = _db.Empleados.FirstOrDefault(e => e.Email.ToLower() == dto.Email.ToLower());

            if (empleado == null)
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
            // Verificar la contraseña
            return BCrypt.Net.BCrypt.Verify(password, passwordHashed);
        }
    }
}
