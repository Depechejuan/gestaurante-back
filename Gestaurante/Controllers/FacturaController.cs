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

        [HttpGet("clientes/search")]
        public async Task<IActionResult> SearchClientes([FromQuery] string? query, CancellationToken cancellationToken)
        {
            return ResponseHelper.SendResponse(await _facturaService.SearchClientesAsync(query, cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearFacturaDTO dto, CancellationToken cancellationToken)
        {
            var factura = await _facturaService.CreateAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(factura, 201);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EditarFacturaDTO dto, CancellationToken cancellationToken)
        {
            var factura = await _facturaService.UpdateAsync(id, dto, cancellationToken);
            return factura == null
                ? ResponseHelper.NotFound("Factura no encontrada.")
                : ResponseHelper.SendResponse(factura);
        }

        [HttpPut("{id:guid}/cliente")]
        public async Task<IActionResult> AssignCliente(Guid id, [FromBody] AsignarFacturaClienteDTO dto, CancellationToken cancellationToken)
        {
            var factura = await _facturaService.AssignClienteAsync(id, dto, cancellationToken);
            return factura == null
                ? ResponseHelper.NotFound("Factura no encontrada.")
                : ResponseHelper.SendResponse(factura);
        }

        [HttpPost("{id:guid}/cobrar")]
        public async Task<IActionResult> Charge(Guid id, [FromBody] CobrarFacturaDTO dto, CancellationToken cancellationToken)
        {
            var factura = await _facturaService.ChargeAsync(id, dto, cancellationToken);
            return factura == null
                ? ResponseHelper.NotFound("Factura no encontrada.")
                : ResponseHelper.SendResponse(factura);
        }

        [HttpPost("{id:guid}/send-email")]
        public async Task<IActionResult> SendEmail(Guid id, [FromBody] SendFacturaEmailDTO? dto, CancellationToken cancellationToken)
        {
            var sentTo = await _facturaService.SendFacturaEmailAsync(id, dto?.Email, cancellationToken);
            return ResponseHelper.SendResponse(new { sent = true, sentTo });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var deleted = await _facturaService.DeleteAsync(id, cancellationToken);
            return deleted
                ? ResponseHelper.SendResponse(new { id, deleted = true })
                : ResponseHelper.NotFound("Factura no encontrada.");
        }
    }
}
