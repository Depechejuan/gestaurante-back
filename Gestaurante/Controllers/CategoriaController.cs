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
    public class CategoriaController : ControllerBase
    {
        private readonly CategoriaService _service;

        public CategoriaController(CategoriaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var categorias = await _service.GetAllAsync(cancellationToken);
            return ResponseHelper.SendResponse(categorias);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var categoria = await _service.GetByIdAsync(id, cancellationToken);
            if (categoria == null)
            {
                return ResponseHelper.NotFound("Categoria no encontrada.");
            }

            return ResponseHelper.SendResponse(categoria);
        }

        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoriaDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var categoria = await _service.CreateAsync(dto, cancellationToken);
                return ResponseHelper.SendResponse(categoria, 201);
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.ValidationError(ex.Message);
            }
        }

        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CategoriaDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var categoria = await _service.UpdateAsync(id, dto, cancellationToken);
                if (categoria == null)
                {
                    return ResponseHelper.NotFound("Categoria no encontrada.");
                }

                return ResponseHelper.SendResponse(categoria);
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
                    return ResponseHelper.NotFound("Categoria no encontrada.");
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
