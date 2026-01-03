using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Models.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Gestaurante.Models.Services;
using Gestaurante.Utils;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly RegisterService _registerService;

        public AdminController(RegisterService registerService)
        {
            _registerService = registerService;
        }


        [HttpPost("register")]
        public IActionResult Register([FromBody] RegistroDTO dto)
        {
            try
            {
                var empleado = _registerService.CrearEmpleado(dto);
                return ResponseHelper.SendResponse( new { id = empleado.Id });
            }
            catch (Exception ex)
            {
                return ResponseHelper.SendError(ex, 500);
            }
        }
    }
}
