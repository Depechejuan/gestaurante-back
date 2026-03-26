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
    public class IngredienteController : ControllerBase
    {
        private readonly IngredienteService _service;

        public IngredienteController(IngredienteService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var ingredientes = await _service.GetAllAsync(cancellationToken);
            return ResponseHelper.SendResponse(ingredientes);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var ingrediente = await _service.GetByIdAsync(id, cancellationToken);
            if (ingrediente == null)
            {
                return ResponseHelper.NotFound("Ingrediente no encontrado.");
            }

            return ResponseHelper.SendResponse(ingrediente);
        }

        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] IngredienteDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var ingrediente = await _service.CreateAsync(dto, cancellationToken);
                return ResponseHelper.SendResponse(ingrediente, 201);
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.ValidationError(ex.Message);
            }
        }

        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] IngredienteDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var ingrediente = await _service.UpdateAsync(id, dto, cancellationToken);
                if (ingrediente == null)
                {
                    return ResponseHelper.NotFound("Ingrediente no encontrado.");
                }

                return ResponseHelper.SendResponse(ingrediente);
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
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return ResponseHelper.NotFound("Ingrediente no encontrado.");
            }

            return ResponseHelper.SendResponse(new { deleted = true });
        }
    }
}
