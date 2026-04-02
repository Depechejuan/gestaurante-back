using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    /// <summary>
    /// Gestiona el catálogo interno de platos y su disponibilidad administrativa.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class PlatoController : ControllerBase
    {
        private readonly PlatoService _service;

        /// <summary>
        /// Inicializa el controlador con el servicio de platos.
        /// </summary>
        public PlatoController(PlatoService service)
        {
            _service = service;
        }

        /// <summary>
        /// Devuelve todos los platos del catálogo interno.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var platos = await _service.GetAllAsync(cancellationToken);
            return ResponseHelper.SendResponse(platos);
        }

        /// <summary>
        /// Recupera el detalle de un plato concreto.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var plato = await _service.GetByIdAsync(id, cancellationToken);
            if (plato == null)
            {
                return ResponseHelper.NotFound("Plato no encontrado.");
            }

            return ResponseHelper.SendResponse(plato);
        }

        /// <summary>
        /// Crea un nuevo plato en el catálogo.
        /// </summary>
        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PlatoDTO dto, CancellationToken cancellationToken)
        {
            var plato = await _service.CreateAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(plato, 201);
        }

        /// <summary>
        /// Actualiza la ficha de un plato existente.
        /// </summary>
        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] PlatoDTO dto, CancellationToken cancellationToken)
        {
            var plato = await _service.UpdateAsync(id, dto, cancellationToken);
            if (plato == null)
                return ResponseHelper.NotFound("Plato no encontrado.");

            return ResponseHelper.SendResponse(plato);
        }

        /// <summary>
        /// Elimina físicamente un plato que no tenga dependencias históricas.
        /// </summary>
        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted)
                return ResponseHelper.NotFound("Plato no encontrado.");

            return ResponseHelper.SendResponse(new { deleted = true });
        }

        /// <summary>
        /// Activa o desactiva la visibilidad operativa de un plato sin alterar su histórico.
        /// </summary>
        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpPatch("{id:guid}/disponibilidad")]
        public async Task<IActionResult> SetDisponibilidad(Guid id, [FromBody] UpdatePlatoDisponibilidadDTO dto, CancellationToken cancellationToken)
        {
            var plato = await _service.SetDisponibilidadAsync(id, dto.Disponible, cancellationToken);
            if (plato == null)
                return ResponseHelper.NotFound("Plato no encontrado.");

            return ResponseHelper.SendResponse(plato);
        }
    }
}
