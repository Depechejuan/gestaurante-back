using System.Security.Claims;
using Gestaurante.Models.Services;
using Gestaurante.Models.DTO;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("public")]
    [Authorize(AuthenticationSchemes = "CustomerBearer")]
    public class PublicCheckoutController : ControllerBase
    {
        private readonly PublicCheckoutService _publicCheckoutService;
        private readonly PedidoService _pedidoService;

        public PublicCheckoutController(PublicCheckoutService publicCheckoutService, PedidoService pedidoService)
        {
            _publicCheckoutService = publicCheckoutService;
            _pedidoService = pedidoService;
        }

        [HttpPost("checkout/order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOnlineOrderDTO dto, CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            var pedido = await _publicCheckoutService.CreateOnlineOrderAsync(clienteId.Value, dto, cancellationToken);
            return ResponseHelper.SendResponse(pedido, 201);
        }

        [HttpGet("account/orders")]
        public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            return ResponseHelper.SendResponse(await _pedidoService.GetByClienteAsync(clienteId.Value, cancellationToken));
        }

        [HttpGet("account/orders/{id:guid}")]
        public async Task<IActionResult> GetOrder(Guid id, CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            var pedido = await _pedidoService.GetByClienteAndIdAsync(clienteId.Value, id, cancellationToken);
            return pedido == null ? ResponseHelper.NotFound("Pedido no encontrado.") : ResponseHelper.SendResponse(pedido);
        }

        private Guid? GetCustomerId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            return Guid.TryParse(value, out var guid) ? guid : null;
        }
    }
}
