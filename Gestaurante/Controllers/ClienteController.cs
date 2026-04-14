using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    /// <summary>
    /// Expone la gestión interna de clientes para administración y sala.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Administrador,Camarero")]
    public class ClienteController : ControllerBase
    {
        private readonly CustomerAccountService _customerAccountService;

        /// <summary>
        /// Inicializa el controlador con el servicio de cuentas de cliente.
        /// </summary>
        public ClienteController(CustomerAccountService customerAccountService)
        {
            _customerAccountService = customerAccountService;
        }

        /// <summary>
        /// Devuelve el listado interno de clientes, opcionalmente filtrado por búsqueda.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? query, CancellationToken cancellationToken)
        {
            return ResponseHelper.SendResponse(await _customerAccountService.GetInternalClientesAsync(query, cancellationToken));
        }

        /// <summary>
        /// Recupera el detalle de un cliente concreto.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var cliente = await _customerAccountService.GetProfileAsync(id, cancellationToken);
            return cliente == null
                ? ResponseHelper.NotFound("Cliente no encontrado.")
                : ResponseHelper.SendResponse(cliente);
        }

        /// <summary>
        /// Crea un cliente interno desde el panel de administración o sala.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInternalClienteDTO dto, CancellationToken cancellationToken)
        {
            var cliente = await _customerAccountService.CreateInternalClienteAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(cliente, 201);
        }

        /// <summary>
        /// Actualiza los datos completos de un cliente interno.
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInternalClienteDTO dto, CancellationToken cancellationToken)
        {
            var cliente = await _customerAccountService.UpdateInternalClienteAsync(id, dto, cancellationToken);
            return cliente == null
                ? ResponseHelper.NotFound("Cliente no encontrado.")
                : ResponseHelper.SendResponse(cliente);
        }

        /// <summary>
        /// Activa o desactiva un cliente para futuros usos operativos.
        /// </summary>
        [HttpPatch("{id:guid}/estado")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ToggleActivo(Guid id, [FromBody] ToggleClienteActivoDTO dto, CancellationToken cancellationToken)
        {
            var cliente = await _customerAccountService.SetActivoAsync(id, dto.Activo, cancellationToken);
            return cliente == null
                ? ResponseHelper.NotFound("Cliente no encontrado.")
                : ResponseHelper.SendResponse(cliente);
        }
    }
}
