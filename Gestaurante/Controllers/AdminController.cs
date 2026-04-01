using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
    public class AdminController : ControllerBase
    {
        private readonly RegisterService _registerService;
        private readonly StaffService _staffService;

        public AdminController(RegisterService registerService, StaffService staffService)
        {
            _registerService = registerService;
            _staffService = staffService;
        }


        [HttpPost("register")]
        //[Consumes("multipart/form-data")]
        // NO ME DEJA REGISTRAR NUEVO USUARIO, PIDE FOTO ?????
        public async Task<IActionResult> Register([FromBody] RegistroDTO dto)
        {
            try
            {
                var empleado = await _registerService.CrearEmpleado(dto);
                return ResponseHelper.SendResponse(new { id = empleado.Id, foto = empleado.ImageURL });
            }
            catch (ValidationException ex)
            {
                return ResponseHelper.ValidationError(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.Conflict(ex.Message);
            }
            catch
            {
                return ResponseHelper.ServerError("No se ha podido registrar el empleado.");
            }
        }

        [HttpPost("getbasicuser")]
        public async Task<IActionResult> GetBasicUser([FromBody] IdRequestDTO user)
        {
            try
            {
                var empleado = await _staffService.GetBasicStaff(user.Id);
                if (empleado == null)
                    return ResponseHelper.NotFound("Empleado no encontrado.");

                return ResponseHelper.SendResponse(empleado);
            }
            catch
            {
                return ResponseHelper.ServerError("No se ha podido recuperar el usuario solicitado.");
            }
        }

        [HttpPost("getusers")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var empleados = await _staffService.GetAllUsers();
                return ResponseHelper.SendResponse(empleados);
            }
            catch
            {
                return ResponseHelper.ServerError("No se ha podido cargar la lista de empleados.");
            }
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetFullUser(Guid id)
        {
            try
            {
                var empleado = await _staffService.GetFullUser(id);
                if (empleado == null)
                    return ResponseHelper.NotFound("Empleado no encontrado.");

                return ResponseHelper.SendResponse(empleado);
            }
            catch (KeyNotFoundException ex)
            {
                return ResponseHelper.NotFound(ex.Message);
            }
            catch
            {
                return ResponseHelper.ServerError("No se ha podido recuperar el detalle del empleado.");
            }
        }

        [HttpPut("user/{id}/photo")]
        public async Task<IActionResult> UpdatePhoto([FromRoute] Guid id, [FromForm] IFormFile file)
        {
            try
            {
                EmpleadoFullDTO empleado = await _staffService.GetFullUser(id);
                if (empleado == null)
                    return ResponseHelper.NotFound("Empleado no encontrado.");

                Empleado empleadoEdit = await _registerService.UploadPhoto(empleado, file, "empleados");
                // actualizar empleadoEdit

                return ResponseHelper.SendResponse(new { foto = empleadoEdit.ImageURL });
            }
            catch (KeyNotFoundException ex)
            {
                return ResponseHelper.NotFound(ex.Message);
            }
            catch
            {
                return ResponseHelper.ServerError("No se ha podido actualizar la foto del empleado.");
            }
        }

        [HttpPut("user/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> GetUniqueUser([FromRoute] Guid id, [FromForm] EditarEmpleadoDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var newEmpleado = await _registerService.EditarEmpleado(id, dto, cancellationToken);
                if (newEmpleado == null)
                    return ResponseHelper.NotFound("Empleado no encontrado.");

                var tipo = dto.Tipo
                    ?? (newEmpleado is Administrador ? TipoEmpleado.Administrador
                    : newEmpleado is Camarero ? TipoEmpleado.Camarero
                    : newEmpleado is Repartidor ? TipoEmpleado.Repartidor
                    : TipoEmpleado.Cocinero);
                var empleado = ToDTO.EmpleadoToEmpleadoFullDTO(newEmpleado, tipo);

                return ResponseHelper.SendResponse(empleado);
            }
            catch (KeyNotFoundException ex)
            {
                return ResponseHelper.NotFound(ex.Message);
            }
            catch (ValidationException ex)
            {
                return ResponseHelper.ValidationError(ex.Message);
            }
            catch
            {
                return ResponseHelper.ServerError("No se ha podido actualizar el empleado.");
            }
        }

    }
}
