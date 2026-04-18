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

        public async Task<Empleado> UploadPhoto(EmpleadoFullDTO dto, IFormFile file, string location)
        {
            var empleado = await _db.Empleados.FindAsync(dto.Id)
                ?? throw new KeyNotFoundException("Empleado no encontrado.");

            empleado.ImageURL = await _employeeImageService.UploadOrReplaceEmployeeImageAsync(empleado.Id, file);
            empleado.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return empleado;
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

            //string imageUrl = string.Empty;

            //if (dto.Imagen != null)
            //    imageUrl = await _cloudinary.UploadImageAsync(dto.Imagen, "empleados");

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
                TipoEmpleado.Repartidor => new Repartidor(
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

            empleado.Activo = true;

            await _db.Empleados.AddAsync(empleado);
            _db.SaveChanges();

            return empleado;
        }

        //public async Task<Empleado> ActualizarEmpleado(Empleado empleado)
        //{
            
        //    await _db.SaveChangesAsync();
        //    return existingEmpleado;
        //}

        public async Task<Empleado?> EditarEmpleado(Guid id, EditarEmpleadoDTO dto, CancellationToken cancellationToken = default)
        {
            var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (empleado == null)
                return null;

            var currentRole = ResolveEmployeeType(empleado);
            var nextRole = dto.Tipo ?? currentRole;
            var nextFirstName = KeepExistingIfBlank(dto.Nombre, empleado.FirstName);
            var nextFirstLastName = KeepExistingIfBlank(dto.Apellido1, empleado.FirstLastName);
            var nextSecondLastName = KeepExistingIfBlank(dto.Apellido2, empleado.SecondLastName);
            var nextEmail = NormalizeEmail(KeepExistingIfBlank(dto.Email, empleado.Email));
            var nextDni = NormalizeDocument(KeepExistingIfBlank(dto.DNI, empleado.DNI));
            var nextNuss = NormalizeDocument(KeepExistingIfBlank(dto.NUSS, empleado.NUSS));

            await EnsureEmployeeIdentityIsUniqueAsync(id, nextEmail, nextDni, nextNuss, cancellationToken);

            empleado.UpdateNames(nextFirstName, nextFirstLastName, nextSecondLastName);
            empleado.UpdateIdentity(nextEmail, nextDni, nextNuss);
            empleado.Activo = dto.Activo;
            empleado.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                empleado.Password = BCrypt.Net.BCrypt.HashPassword(
                    dto.Password,
                    BCrypt.Net.BCrypt.GenerateSalt(12)
                );
            }

            if (dto.Photo is { Length: > 0 })
                empleado.ImageURL = await _employeeImageService.UploadOrReplaceEmployeeImageAsync(id, dto.Photo, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            if (nextRole != currentRole)
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE ""Empleados""
                    SET ""Tipo"" = {(int)nextRole}
                    WHERE ""Id"" = {id};
                ", cancellationToken);

                _db.ChangeTracker.Clear();
            }

            return await _db.Empleados.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        private static string KeepExistingIfBlank(string? incomingValue, string currentValue)
        {
            return string.IsNullOrWhiteSpace(incomingValue) ? currentValue : incomingValue.Trim();
        }

        private async Task EnsureEmployeeIdentityIsUniqueAsync(Guid employeeId, string email, string dni, string nuss, CancellationToken cancellationToken)
        {
            if (await _db.Empleados.AnyAsync(
                empleado => empleado.Id != employeeId && empleado.Email.ToLower() == email.ToLower(),
                cancellationToken))
                throw new InvalidOperationException("Ya existe otro empleado con ese email.");

            if (await _db.Empleados.AnyAsync(
                empleado => empleado.Id != employeeId && empleado.DNI.ToUpper() == dni.ToUpper(),
                cancellationToken))
                throw new InvalidOperationException("Ya existe otro empleado con ese DNI.");

            if (await _db.Empleados.AnyAsync(
                empleado => empleado.Id != employeeId && empleado.NUSS.ToUpper() == nuss.ToUpper(),
                cancellationToken))
                throw new InvalidOperationException("Ya existe otro empleado con ese NUSS.");
        }

        private static string NormalizeEmail(string value)
        {
            return value.Trim();
        }

        private static string NormalizeDocument(string value)
        {
            return value.Trim().ToUpperInvariant();
        }

        private static TipoEmpleado ResolveEmployeeType(Empleado empleado)
        {
            if (empleado is Administrador) return TipoEmpleado.Administrador;
            if (empleado is Camarero) return TipoEmpleado.Camarero;
            if (empleado is Repartidor) return TipoEmpleado.Repartidor;
            return TipoEmpleado.Cocinero;
        }
    }
}
