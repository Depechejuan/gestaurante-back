using System.ComponentModel.DataAnnotations;
using BCrypt.Net;
using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class RegisterService
    {
        private readonly AppDbContext _db;
        private readonly IEmployeeImageService _employeeImageService;

        public RegisterService(AppDbContext db, IEmployeeImageService employeeImageService)
        {
            _db = db;
            _employeeImageService = employeeImageService;
        }

        public async Task<Empleado> CrearEmpleado(RegistroDTO dto)
        {
            // Esta línea llama a Bcrypt para hashear el password, y es el parámetro que se envía al constructor.
            // Así podemos almacenar la contraseña hasheada y si hay una vulnerabilidad, no se podrá obtener la contraseña real.
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(
                dto.Password,
                BCrypt.Net.BCrypt.GenerateSalt(12)
            );
            // se añade una "sal" para más complejidad de hasheo y que sea más difícil de romper.

            // Esto es un switch dependiendo del tipo, así empleado se crea dependiendo del caso
            Empleado empleado = dto.Tipo switch
            {
                TipoEmpleado.Administrador => new Administrador(
                    dto.Email,
                    hashedPassword,
                    dto.FirstName,
                    dto.FirstLastName,
                    dto.SecondLastName,
                    dto.DNI,
                    dto.NUSS

                ),
                TipoEmpleado.Camarero => new Camarero(
                    dto.Email,
                    hashedPassword,
                    dto.FirstName,
                    dto.FirstLastName,
                    dto.SecondLastName,
                    dto.DNI,
                    dto.NUSS
                ),
                TipoEmpleado.Cocinero => new Cocinero(
                    dto.Email,
                    hashedPassword,
                    dto.FirstName,
                    dto.FirstLastName,
                    dto.SecondLastName,
                    dto.DNI,
                    dto.NUSS
                ),
                _ => throw new ValidationException("Tipo de empleado no válido")
            };

            await _db.Empleados.AddAsync(empleado);
            _db.SaveChanges();

            return empleado;
        }


        public async Task<Empleado?> EditarEmpleado(Guid id, EditarEmpleadoDTO dto, CancellationToken cancellationToken = default)
        {
            var oldEmpleado = await _db.Empleados.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (oldEmpleado == null)
                return null;

            var updatedAt = DateTime.UtcNow;
            var currentRole = ResolveEmployeeType(oldEmpleado);
            var nextRole = dto.Tipo ?? currentRole;
            var nextFirstName = KeepExistingIfBlank(dto.Nombre, oldEmpleado.FirstName);
            var nextFirstLastName = KeepExistingIfBlank(dto.Apellido1, oldEmpleado.FirstLastName);
            var nextSecondLastName = KeepExistingIfBlank(dto.Apellido2, oldEmpleado.SecondLastName);
            var nextEmail = KeepExistingIfBlank(dto.Email, oldEmpleado.Email);
            var nextDni = KeepExistingIfBlank(dto.DNI, oldEmpleado.DNI);
            var nextNuss = KeepExistingIfBlank(dto.NUSS, oldEmpleado.NUSS);
            var nextImageUrl = oldEmpleado.ImageURL;
            var nextPasswordHash = oldEmpleado.Password;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                nextPasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    dto.Password,
                    BCrypt.Net.BCrypt.GenerateSalt(12)
                );
            }

            if (dto.Photo is { Length: > 0 })
            {
                nextImageUrl = await _employeeImageService.UploadOrReplaceEmployeeImageAsync(id, dto.Photo, cancellationToken);
            }

            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""Empleados""
                SET
                    ""Email"" = {nextEmail},
                    ""Password"" = {nextPasswordHash},
                    ""FirstName"" = {nextFirstName},
                    ""FirstLastName"" = {nextFirstLastName},
                    ""SecondLastName"" = {nextSecondLastName},
                    ""DNI"" = {nextDni},
                    ""NUSS"" = {nextNuss},
                    ""ImageURL"" = {nextImageUrl},
                    ""UpdatedAt"" = {updatedAt},
                    ""Tipo"" = {(int)nextRole}
                WHERE ""Id"" = {id};
            ", cancellationToken);

            return await _db.Empleados.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        private static string KeepExistingIfBlank(string? incomingValue, string currentValue)
        {
            return string.IsNullOrWhiteSpace(incomingValue) ? currentValue : incomingValue.Trim();
        }

        private static TipoEmpleado ResolveEmployeeType(Empleado empleado)
        {
            if (empleado is Administrador) return TipoEmpleado.Administrador;
            if (empleado is Camarero) return TipoEmpleado.Camarero;
            return TipoEmpleado.Cocinero;
        }
    }
}
