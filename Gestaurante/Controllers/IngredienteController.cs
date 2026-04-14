using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    /// <summary>
    /// Expone operaciones CRUD para los ingredientes disponibles en catálogo.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class IngredienteController : ControllerBase
    {
        private readonly IngredienteService _service;

        /// <summary>
        /// Inicializa el controlador con el servicio de ingredientes.
        /// </summary>
        public IngredienteController(IngredienteService service)
        {
            _service = service;
        }

        /// <summary>
        /// Obtiene todos los ingredientes registrados.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var ingredientes = await _service.GetAllAsync(cancellationToken);
            return ResponseHelper.SendResponse(ingredientes);
        }

        /// <summary>
        /// Recupera un ingrediente concreto mediante su identificador.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var ingrediente = await _service.GetByIdAsync(id, cancellationToken);
            if (ingrediente == null)
                return ResponseHelper.NotFound("Ingrediente no encontrado.");

            return ResponseHelper.SendResponse(ingrediente);
        }

        /// <summary>
        /// Crea un nuevo ingrediente en el catálogo.
        /// </summary>
        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] IngredienteDTO dto, CancellationToken cancellationToken)
        {
            var ingrediente = await _service.CreateAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(ingrediente, 201);
        }

        /// <summary>
        /// Actualiza la información de un ingrediente existente.
        /// </summary>
        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] IngredienteDTO dto, CancellationToken cancellationToken)
        {
            var ingrediente = await _service.UpdateAsync(id, dto, cancellationToken);
            if (ingrediente == null)
                return ResponseHelper.NotFound("Ingrediente no encontrado.");

            return ResponseHelper.SendResponse(ingrediente);
        }

        /// <summary>
        /// Elimina un ingrediente si no está asociado a platos.
        /// </summary>
        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted)
                return ResponseHelper.NotFound("Ingrediente no encontrado.");

            return ResponseHelper.SendResponse(new { deleted = true });
        }
    }
}
