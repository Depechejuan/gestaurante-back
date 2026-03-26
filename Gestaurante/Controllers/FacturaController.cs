using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Administrador,Camarero")]
    public class FacturaController : ControllerBase
    {
        private readonly FacturaService _facturaService;

        public FacturaController(FacturaService facturaService)
        {
            _facturaService = facturaService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var facturas = await _facturaService.GetAllAsync(cancellationToken);
            return ResponseHelper.SendResponse(facturas);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var factura = await _facturaService.GetByIdAsync(id, cancellationToken);
            return factura == null
                ? ResponseHelper.NotFound("Factura no encontrada.")
                : ResponseHelper.SendResponse(factura);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearFacturaDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var factura = await _facturaService.CreateAsync(dto, cancellationToken);
                return ResponseHelper.SendResponse(factura, 201);
            }
            catch (KeyNotFoundException ex)
            {
                return ResponseHelper.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.ValidationError(ex.Message);
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EditarFacturaDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var factura = await _facturaService.UpdateAsync(id, dto, cancellationToken);
                return factura == null
                    ? ResponseHelper.NotFound("Factura no encontrada.")
                    : ResponseHelper.SendResponse(factura);
            }
            catch (KeyNotFoundException ex)
            {
                return ResponseHelper.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.Conflict(ex.Message);
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var deleted = await _facturaService.DeleteAsync(id, cancellationToken);
                return deleted
                    ? ResponseHelper.SendResponse(new { id, deleted = true })
                    : ResponseHelper.NotFound("Factura no encontrada.");
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.Conflict(ex.Message);
            }
        }
    }
}
