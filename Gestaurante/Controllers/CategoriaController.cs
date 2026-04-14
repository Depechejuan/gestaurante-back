using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    /// <summary>
    /// Expone operaciones CRUD para la gestión de categorías de la carta.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class CategoriaController : ControllerBase
    {
        private readonly CategoriaService _service;

        /// <summary>
        /// Inicializa el controlador con el servicio de categorías.
        /// </summary>
        public CategoriaController(CategoriaService service)
        {
            _service = service;
        }

        /// <summary>
        /// Obtiene todas las categorías registradas.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var categorias = await _service.GetAllAsync(cancellationToken);
            return ResponseHelper.SendResponse(categorias);
        }

        /// <summary>
        /// Obtiene una categoría concreta a partir de su identificador.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var categoria = await _service.GetByIdAsync(id, cancellationToken);
            if (categoria == null)
                return ResponseHelper.NotFound("Categoria no encontrada.");

            return ResponseHelper.SendResponse(categoria);
        }

        /// <summary>
        /// Crea una nueva categoría de carta.
        /// </summary>
        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoriaDTO dto, CancellationToken cancellationToken)
        {
            var categoria = await _service.CreateAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(categoria, 201);
        }

        /// <summary>
        /// Actualiza los datos de una categoría existente.
        /// </summary>
        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CategoriaDTO dto, CancellationToken cancellationToken)
        {
            var categoria = await _service.UpdateAsync(id, dto, cancellationToken);
            if (categoria == null)
                return ResponseHelper.NotFound("Categoria no encontrada.");

            return ResponseHelper.SendResponse(categoria);
        }

        /// <summary>
        /// Elimina una categoría si no tiene platos asociados.
        /// </summary>
        [Authorize(Roles = nameof(TipoEmpleado.Administrador))]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted)
                return ResponseHelper.NotFound("Categoria no encontrada.");

            return ResponseHelper.SendResponse(new { deleted = true });
        }
    }
}
