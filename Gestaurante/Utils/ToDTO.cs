using Microsoft.AspNetCore.StaticAssets;

namespace Gestaurante.Utils
{
    public class ToDTO
    {
        public static Models.DTO.EmpleadoFullDTO EmpleadoToEmpleadoFullDTO(Models.Entities.Empleado empleado, Models.DTO.TipoEmpleado tipo)
        {
            return new Models.DTO.EmpleadoFullDTO(
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
                CreatedAt = empleado.CreatedAt,
                UpdatedAt = empleado.UpdatedAt
            };
        }
    }
}
