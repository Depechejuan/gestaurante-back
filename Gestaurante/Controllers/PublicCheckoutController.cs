using System.Security.Claims;
using Gestaurante.Models.Services;
using Gestaurante.Models.DTO;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    /// <summary>
    /// Gestiona el checkout autenticado de cliente para pedidos online.
    /// </summary>
    [ApiController]
    [Route("public")]
    [Authorize(AuthenticationSchemes = "CustomerBearer")]
    public class PublicCheckoutController : ControllerBase
    {
        private readonly PublicCheckoutService _publicCheckoutService;
        private readonly PedidoService _pedidoService;

        /// <summary>
        /// Inicializa el controlador con los servicios de checkout y pedidos.
        /// </summary>
        /// <param name="publicCheckoutService">Servicio que orquesta el checkout online autenticado.</param>
        /// <param name="pedidoService">Servicio de consulta del histórico de pedidos del cliente.</param>
        public PublicCheckoutController(PublicCheckoutService publicCheckoutService, PedidoService pedidoService)
        {
            _publicCheckoutService = publicCheckoutService;
            _pedidoService = pedidoService;
        }

        /// <summary>
        /// Crea un pedido online para el cliente autenticado.
        /// </summary>
        /// <param name="dto">Datos del checkout, incluyendo líneas, entrega y pago.</param>
        /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
        /// <returns>Respuesta HTTP con el pedido creado o un error de autorización si no hay cliente válido.</returns>
        [HttpPost("checkout/order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOnlineOrderDTO dto, CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            var pedido = await _publicCheckoutService.CreateOnlineOrderAsync(clienteId.Value, dto, cancellationToken);
            return ResponseHelper.SendResponse(pedido, 201);
        }

        /// <summary>
        /// Devuelve el histórico de pedidos online del cliente autenticado.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
        /// <returns>Respuesta HTTP con la colección de pedidos del cliente autenticado.</returns>
        [HttpGet("account/orders")]
        public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            return ResponseHelper.SendResponse(await _pedidoService.GetByClienteAsync(clienteId.Value, cancellationToken));
        }

        /// <summary>
        /// Recupera un pedido online concreto perteneciente al cliente autenticado.
        /// </summary>
        /// <param name="id">Identificador del pedido solicitado.</param>
        /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
        /// <returns>Respuesta HTTP con el pedido solicitado o un 404 si no pertenece al cliente.</returns>
        [HttpGet("account/orders/{id:guid}")]
        public async Task<IActionResult> GetOrder(Guid id, CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            var pedido = await _pedidoService.GetByClienteAndIdAsync(clienteId.Value, id, cancellationToken);
            return pedido == null ? ResponseHelper.NotFound("Pedido no encontrado.") : ResponseHelper.SendResponse(pedido);
        }

        /// <summary>
        /// Obtiene el identificador del cliente autenticado a partir del token público.
        /// </summary>
        /// <returns>Identificador del cliente si el token es válido; en otro caso, <see langword="null"/>.</returns>
        private Guid? GetCustomerId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            return Guid.TryParse(value, out var guid) ? guid : null;
        }
    }
}
