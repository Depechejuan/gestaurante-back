using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    /// <summary>
    /// Gestiona facturas, cobros y asignación fiscal desde el panel interno.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Administrador,Camarero")]
    public class FacturaController : ControllerBase
    {
        private readonly FacturaService _facturaService;

        /// <summary>
        /// Inicializa el controlador con el servicio de facturación.
        /// </summary>
        /// <param name="facturaService">Servicio que encapsula la lógica de facturas y cobros.</param>
        public FacturaController(FacturaService facturaService)
        {
            _facturaService = facturaService;
        }

        /// <summary>
        /// Devuelve todas las facturas registradas.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
        /// <returns>Respuesta HTTP con el listado de facturas.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var facturas = await _facturaService.GetAllAsync(cancellationToken);
            return ResponseHelper.SendResponse(facturas);
        }

        /// <summary>
        /// Recupera una factura concreta con su detalle completo.
        /// </summary>
        /// <param name="id">Identificador de la factura.</param>
        /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
        /// <returns>Respuesta HTTP con la factura solicitada o un 404 si no existe.</returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var factura = await _facturaService.GetByIdAsync(id, cancellationToken);
            return factura == null
                ? ResponseHelper.NotFound("Factura no encontrada.")
                : ResponseHelper.SendResponse(factura);
        }

        /// <summary>
        /// Busca clientes que se pueden vincular a una factura.
        /// </summary>
        /// <param name="query">Texto libre para buscar por email, nombre o documento.</param>
        /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
        /// <returns>Respuesta HTTP con clientes compatibles con la asignación fiscal.</returns>
        [HttpGet("clientes/search")]
        public async Task<IActionResult> SearchClientes([FromQuery] string? query, CancellationToken cancellationToken)
        {
            return ResponseHelper.SendResponse(await _facturaService.SearchClientesAsync(query, cancellationToken));
        }

        /// <summary>
        /// Crea una factura a partir de una mesa, un pedido o un importe manual.
        /// </summary>
        /// <param name="dto">Datos necesarios para la creación de la factura.</param>
        /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
        /// <returns>Respuesta HTTP 201 con la factura creada.</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearFacturaDTO dto, CancellationToken cancellationToken)
        {
            var factura = await _facturaService.CreateAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(factura, 201);
        }

        /// <summary>
        /// Actualiza los datos editables de una factura existente.
        /// </summary>
        /// <param name="id">Identificador de la factura a modificar.</param>
        /// <param name="dto">Cambios aplicables a la factura.</param>
        /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
        /// <returns>Respuesta HTTP con la factura actualizada o un 404 si no existe.</returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EditarFacturaDTO dto, CancellationToken cancellationToken)
        {
            var factura = await _facturaService.UpdateAsync(id, dto, cancellationToken);
            return factura == null
                ? ResponseHelper.NotFound("Factura no encontrada.")
                : ResponseHelper.SendResponse(factura);
        }

        /// <summary>
        /// Asigna o cambia el cliente fiscal asociado a una factura.
        /// </summary>
        /// <param name="id">Identificador de la factura.</param>
        /// <param name="dto">Datos del cliente o snapshot fiscal a aplicar.</param>
        /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
        /// <returns>Respuesta HTTP con la factura actualizada o un 404 si no existe.</returns>
        [HttpPut("{id:guid}/cliente")]
        public async Task<IActionResult> AssignCliente(Guid id, [FromBody] AsignarFacturaClienteDTO dto, CancellationToken cancellationToken)
        {
            var factura = await _facturaService.AssignClienteAsync(id, dto, cancellationToken);
            return factura == null
                ? ResponseHelper.NotFound("Factura no encontrada.")
                : ResponseHelper.SendResponse(factura);
        }

        /// <summary>
        /// Registra el cobro de una factura y calcula el cambio si procede.
        /// </summary>
        /// <param name="id">Identificador de la factura a cobrar.</param>
        /// <param name="dto">Datos del método de pago y del efectivo entregado, si aplica.</param>
        /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
        /// <returns>Respuesta HTTP con la factura cobrada o un 404 si no existe.</returns>
        [HttpPost("{id:guid}/cobrar")]
        public async Task<IActionResult> Charge(Guid id, [FromBody] CobrarFacturaDTO dto, CancellationToken cancellationToken)
        {
            var factura = await _facturaService.ChargeAsync(id, dto, cancellationToken);
            return factura == null
                ? ResponseHelper.NotFound("Factura no encontrada.")
                : ResponseHelper.SendResponse(factura);
        }

        /// <summary>
        /// Envía una factura por correo al destinatario indicado o al del cliente vinculado.
        /// </summary>
        /// <param name="id">Identificador de la factura.</param>
        /// <param name="dto">Email opcional de destino para facturas anónimas.</param>
        /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
        /// <returns>Respuesta HTTP con el email final al que se envió la factura.</returns>
        [HttpPost("{id:guid}/send-email")]
        public async Task<IActionResult> SendEmail(Guid id, [FromBody] SendFacturaEmailDTO? dto, CancellationToken cancellationToken)
        {
            var sentTo = await _facturaService.SendFacturaEmailAsync(id, dto?.Email, cancellationToken);
            return ResponseHelper.SendResponse(new { sent = true, sentTo });
        }

        /// <summary>
        /// Elimina una factura siempre que no tenga pedidos asociados.
        /// </summary>
        /// <param name="id">Identificador de la factura.</param>
        /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
        /// <returns>Respuesta HTTP indicando si la factura se eliminó correctamente.</returns>
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
