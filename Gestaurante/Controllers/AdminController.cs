using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Models.Services;
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
        public IActionResult Register([FromBody] RegistroDTO dto)
        {
            try
            {
                var empleado = _registerService.CrearEmpleado(dto);
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
        public IActionResult GetBasicUser([FromBody] IdRequestDTO user)
        {
            try
            {
                var empleado = _staffService.GetBasicStaff(user.Id);
                if (empleado == null)
                {
                    throw new Exception("Empleado no encontrado");
                }
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
    }
}
