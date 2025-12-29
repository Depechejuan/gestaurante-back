using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdminController : ControllerBase
    {
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegistroDTO dto)
        {


            return Ok(new
            {
                empleado.Id,
                empleado.Email
            });
        }


    }
}
