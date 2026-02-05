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
    [Authorize]
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
        public async Task<IActionResult> Register([FromBody] RegistroDTO dto)
        {
            try
            {
                var empleado = await _registerService.CrearEmpleado(dto);
                return ResponseHelper.SendResponse(new { id = empleado.Id });
            }
            catch (Exception ex)
            {
                return ResponseHelper.SendError(new
                {
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                }, 500);
            }
        }

        [HttpPost("getbasicuser")]
        public async Task<IActionResult> GetBasicUser([FromBody] IdRequestDTO user)
        {
            try
            {
                var empleado = await _staffService.GetBasicStaff(user.Id);
                if (empleado == null)
                    throw new Exception("Empleado no encontrado");
                return ResponseHelper.SendResponse(empleado);
            }
            catch (Exception ex)
            {
                return ResponseHelper.SendError(new
                {
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                }, 500);
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
            catch (Exception ex)
            {
                return ResponseHelper.SendError(new
                {
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                }, 500);
            }
        }

        [HttpGet("user/{id}")]
        public IActionResult GetUniqueUser(Guid id)
        {
            try
            {
                var obj = new
                {
                    id
                };
                var empleado = _staffService.GetFullUser(id);
                return ResponseHelper.SendResponse(empleado);
            }
            catch (Exception ex)
            {
                return ResponseHelper.SendError(new
                {
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                }, 500);
            }
        }

        [HttpPut("user/{id}")]
        public async Task<IActionResult> GetUniqueUser([FromRoute] Guid id, [FromBody] EmpleadoFullDTO dto)
        {
            try
            {
                var newEmpleado = await _registerService.EditarEmpleado(dto);
                var empleado = ToDTO.EmpleadoToEmpleadoFullDTO(newEmpleado, dto.Tipo);
                return ResponseHelper.SendResponse(empleado);
            } catch (Exception ex)
            {
                return ResponseHelper.SendError(new
                {
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                }, 500);
            }


        }

    }
}
