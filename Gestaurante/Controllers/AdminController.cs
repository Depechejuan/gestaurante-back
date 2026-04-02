using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Controllers
{
    /// <summary>
    /// Centraliza operaciones administrativas sobre empleados y configuración interna.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
    public class AdminController : ControllerBase
    {
        private readonly RegisterService _registerService;
        private readonly StaffService _staffService;

        /// <summary>
        /// Inicializa el controlador con los servicios de registro y staff.
        /// </summary>
        public AdminController(RegisterService registerService, StaffService staffService)
        {
            _registerService = registerService;
            _staffService = staffService;
        }


        /// <summary>
        /// Registra un nuevo empleado interno.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistroDTO dto)
        {
            var empleado = await _registerService.CrearEmpleado(dto);
            return ResponseHelper.SendResponse(new { id = empleado.Id, foto = empleado.ImageURL });
        }

        /// <summary>
        /// Recupera la información básica de un empleado concreto.
        /// </summary>
        [HttpPost("getbasicuser")]
        public async Task<IActionResult> GetBasicUser([FromBody] IdRequestDTO user)
        {
            var empleado = await _staffService.GetBasicStaff(user.Id);
            if (empleado == null)
                return ResponseHelper.NotFound("Empleado no encontrado.");

            return ResponseHelper.SendResponse(empleado);
        }

        /// <summary>
        /// Devuelve el listado completo de empleados.
        /// </summary>
        [HttpPost("getusers")]
        public async Task<IActionResult> GetUsers()
        {
            var empleados = await _staffService.GetAllUsers();
            return ResponseHelper.SendResponse(empleados);
        }

        /// <summary>
        /// Recupera la ficha completa de un empleado por identificador.
        /// </summary>
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetFullUser(Guid id)
        {
            var empleado = await _staffService.GetFullUser(id);
            if (empleado == null)
                return ResponseHelper.NotFound("Empleado no encontrado.");

            return ResponseHelper.SendResponse(empleado);
        }

        /// <summary>
        /// Sustituye la fotografía de un empleado existente.
        /// </summary>
        [HttpPut("user/{id}/photo")]
        public async Task<IActionResult> UpdatePhoto([FromRoute] Guid id, [FromForm] IFormFile file)
        {
            EmpleadoFullDTO empleado = await _staffService.GetFullUser(id);
            if (empleado == null)
                return ResponseHelper.NotFound("Empleado no encontrado.");

            Empleado empleadoEdit = await _registerService.UploadPhoto(empleado, file, "empleados");
            return ResponseHelper.SendResponse(new { foto = empleadoEdit.ImageURL });
        }

        /// <summary>
        /// Actualiza la ficha completa de un empleado interno.
        /// </summary>
        [HttpPut("user/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> GetUniqueUser([FromRoute] Guid id, [FromForm] EditarEmpleadoDTO dto, CancellationToken cancellationToken)
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

    }
}
