using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;

namespace Gestaurante.Models.Services
{
    public class AuthService
    {
        private readonly AppDbContext _db;

        public AuthService(AppDbContext db)
        {
            _db = db;
        }

        public Empleado? Login(LoginDTO dto)
        {
            return _db.Empleados
                .FirstOrDefault(e => e.Email == dto.Email);
        }
    }
}
