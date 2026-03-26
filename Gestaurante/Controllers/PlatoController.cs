using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlatoController : ControllerBase
    {
        private readonly PlatoService _service;

        public PlatoController(PlatoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var platos = await _service.GetAllAsync(cancellationToken);
            return ResponseHelper.SendResponse(platos);
        }

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

        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PlatoDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var plato = await _service.CreateAsync(dto, cancellationToken);
                return ResponseHelper.SendResponse(plato, 201);
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.ValidationError(ex.Message);
            }
        }

        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] PlatoDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var plato = await _service.UpdateAsync(id, dto, cancellationToken);
                if (plato == null)
                {
                    return ResponseHelper.NotFound("Plato no encontrado.");
                }

                return ResponseHelper.SendResponse(plato);
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.ValidationError(ex.Message);
            }
        }

        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var deleted = await _service.DeleteAsync(id, cancellationToken);
                if (!deleted)
                {
                    return ResponseHelper.NotFound("Plato no encontrado.");
                }

                return ResponseHelper.SendResponse(new { deleted = true });
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.Conflict(ex.Message);
            }
        }
    }
}
