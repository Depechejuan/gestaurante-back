using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;

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
            // Esto es un switch dependiendo del tipo, así empleado se crea dependiendo del caso

            Empleado empleado = dto.Tipo switch
            {
                TipoEmpleado.Administrador => new Administrador(
                    dto.Email,
                    dto.Password,
                    dto.FirstName,
                    dto.FirstLastName,
                    dto.SecondLastName,
                    dto.DNI,
                    dto.NUSS

                ),
                TipoEmpleado.Camarero => new Camarero(
                    dto.Email,
                    dto.Password,
                    dto.FirstName,
                    dto.FirstLastName,
                    dto.SecondLastName,
                    dto.DNI,
                    dto.NUSS
                ),
                TipoEmpleado.Cocinero => new Cocinero(
                    dto.Email,
                    dto.Password,
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
    }
}
