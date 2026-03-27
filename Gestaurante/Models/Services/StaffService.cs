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
                var tipo = ResolveEmployeeType(empleado);

                var dto = new EmpleadoFullDTO(
                    empleado.Id,
                    empleado.FirstName,
                    empleado.FirstLastName,
                    empleado.SecondLastName,
                    empleado.Email,
                    empleado.DNI,
                    empleado.NUSS,
                    tipo
                )
                {
                    Activo = empleado.Activo,
                    ImageURL = NormalizeEmployeeImageUrl(empleado.ImageURL),
                    CreatedAt = empleado.CreatedAt,
                    UpdatedAt = empleado.UpdatedAt
                };

                empleadosDto.Add(dto);
            }

            return empleadosDto;
        }


        public async Task<EmpleadoBasicDTO?> GetBasicStaff(Guid id)
        {
            var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == id);
            if (empleado == null)
                return null;

            var tipo = ResolveEmployeeType(empleado);

            return new EmpleadoBasicDTO(
                    empleado.Id,
                    tipo
                );
        }

        public async Task<EmpleadoFullDTO> GetFullUser(Guid id)
        {
            var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == id);
            if (empleado == null)
                throw new KeyNotFoundException("Empleado no encontrado.");

            var tipo = ResolveEmployeeType(empleado);

            return new EmpleadoFullDTO(empleado.Id, empleado.FirstName, empleado.FirstLastName, empleado.SecondLastName, empleado.Email, empleado.DNI, empleado.NUSS, tipo)
            {
                Activo = empleado.Activo,
                ImageURL = NormalizeEmployeeImageUrl(empleado.ImageURL),
                CreatedAt = empleado.CreatedAt,
                UpdatedAt = empleado.UpdatedAt
            };
        }

        private static string NormalizeEmployeeImageUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return string.Empty;

            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return imageUrl;
            }

            var cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")
                ?? Environment.GetEnvironmentVariable("CLOUDINARY_CLOUDNAME");
            if (string.IsNullOrWhiteSpace(cloudName))
                return imageUrl;

            return $"https://res.cloudinary.com/{cloudName}/image/upload/{imageUrl.TrimStart('/')}";
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
