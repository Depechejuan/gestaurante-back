using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using BCrypt.Net;

namespace Gestaurante.Models.Services
{
    public class RegisterService
    {
        private readonly AppDbContext _db;

        public RegisterService(AppDbContext db)
        {
            _db = db;
        }

        public Empleado CrearEmpleado(RegistroDTO dto)
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

            _db.Empleados.Add(empleado);
            _db.SaveChanges();

            return empleado;
        }


        public async Task<Empleado?> EditarEmpleado(EmpleadoFullDTO dto)
        {

            var oldEmpleado = await _db.Empleados.FindAsync(dto.Id);
            if (oldEmpleado == null)
                return null;

            if (oldEmpleado.FirstName != dto.Nombre && dto.Nombre != null)
                oldEmpleado.FirstName = dto.Nombre;
            if (oldEmpleado.FirstLastName != dto.Apellido1 && dto.Apellido1 != null)
                oldEmpleado.FirstLastName = dto.Apellido1;
            if (oldEmpleado.SecondLastName != dto.Apellido2 && dto.Apellido2 != null)
                oldEmpleado.SecondLastName = dto.Apellido2;
            if (oldEmpleado.NUSS != dto.NUSS && dto.NUSS != null)
                oldEmpleado.NUSS = dto.NUSS;
            if (oldEmpleado.DNI != dto.DNI && dto.DNI != null)
                oldEmpleado.DNI = dto.DNI;
            oldEmpleado.UpdatedAt = DateTime.UtcNow;


            string password = BCrypt.Net.BCrypt.HashPassword(
                    dto.Password,
                    BCrypt.Net.BCrypt.GenerateSalt(12)
                );

            if (oldEmpleado.Password != password && dto.Password != null)
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(
                    dto.Password,
                    BCrypt.Net.BCrypt.GenerateSalt(12)
                );
                oldEmpleado.Password = hashedPassword;
            }

            await _db.SaveChangesAsync();
            return oldEmpleado;
        }
    }
}
