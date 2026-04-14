using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    /// <summary>
    /// Gestiona el flujo público de pedidos por QR asociados a una mesa.
    /// </summary>
    [ApiController]
    [Route("public/mesa")]
    public class PublicMesaController : ControllerBase
    {
        private readonly MesaPublicSessionService _mesaPublicSessionService;

        /// <summary>
        /// Inicializa el controlador con el servicio de sesiones públicas de mesa.
        /// </summary>
        public PublicMesaController(MesaPublicSessionService mesaPublicSessionService)
        {
            _mesaPublicSessionService = mesaPublicSessionService;
        }

        /// <summary>
        /// Abre o recupera una sesión pública para una mesa accesible por QR.
        /// </summary>
        [HttpPost("{id}/session")]
        public async Task<IActionResult> OpenSession(string id, [FromBody] OpenMesaPublicSessionDTO? dto, CancellationToken cancellationToken)
        {
            var mesaId = await _mesaPublicSessionService.ResolveMesaPublicIdAsync(id, cancellationToken);
            var session = await _mesaPublicSessionService.OpenOrResumeAsync(mesaId, dto?.SessionToken, cancellationToken);
            return ResponseHelper.SendResponse(session, 201);
        }

        /// <summary>
        /// Recupera los pedidos vinculados a la sesión pública actual de una mesa.
        /// </summary>
        [HttpGet("{id}/pedidos")]
        public async Task<IActionResult> GetMyPedidos(string id, [FromHeader(Name = "X-Mesa-Session")] string? sessionToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                return ResponseHelper.NotAuthorized("Debes indicar una sesión pública válida.");

            var mesaId = await _mesaPublicSessionService.ResolveMesaPublicIdAsync(id, cancellationToken);
            var pedidos = await _mesaPublicSessionService.GetPedidosAsync(mesaId, sessionToken, cancellationToken);
            return ResponseHelper.SendResponse(pedidos);
        }

        /// <summary>
        /// Crea un pedido público para la mesa asociada al QR y a la sesión activa.
        /// </summary>
        [HttpPost("{id}/pedido")]
        public async Task<IActionResult> CreatePedido(string id, [FromHeader(Name = "X-Mesa-Session")] string? sessionToken, [FromBody] CrearPedidoPublicoDTO dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                return ResponseHelper.NotAuthorized("Debes indicar una sesión pública válida.");

            var mesaId = await _mesaPublicSessionService.ResolveMesaPublicIdAsync(id, cancellationToken);
            var pedido = await _mesaPublicSessionService.CreatePedidoAsync(mesaId, sessionToken, dto, cancellationToken);
            return ResponseHelper.SendResponse(pedido, 201);
        }
    }
}
