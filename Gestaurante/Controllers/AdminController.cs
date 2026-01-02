using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Models.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Gestaurante.Models.Services;

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
                return Ok(new
                {
                    empleado.Id
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor",
                    detalle = ex.Message
                });
            }
    }
}
