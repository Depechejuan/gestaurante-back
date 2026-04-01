using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("public/mesa")]
    public class PublicMesaController : ControllerBase
    {
        private readonly MesaPublicSessionService _mesaPublicSessionService;

        public PublicMesaController(MesaPublicSessionService mesaPublicSessionService)
        {
            _mesaPublicSessionService = mesaPublicSessionService;
        }

        [HttpPost("{id}/session")]
        public async Task<IActionResult> OpenSession(string id, [FromBody] OpenMesaPublicSessionDTO? dto, CancellationToken cancellationToken)
        {
            try
            {
                var mesaId = await _mesaPublicSessionService.ResolveMesaPublicIdAsync(id, cancellationToken);
                var session = await _mesaPublicSessionService.OpenOrResumeAsync(mesaId, dto?.SessionToken, cancellationToken);
                return ResponseHelper.SendResponse(session, 201);
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

        [HttpGet("{id}/pedidos")]
        public async Task<IActionResult> GetMyPedidos(string id, [FromHeader(Name = "X-Mesa-Session")] string? sessionToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                return ResponseHelper.NotAuthorized("Debes indicar una sesión pública válida.");

            try
            {
                var mesaId = await _mesaPublicSessionService.ResolveMesaPublicIdAsync(id, cancellationToken);
                var pedidos = await _mesaPublicSessionService.GetPedidosAsync(mesaId, sessionToken, cancellationToken);
                return ResponseHelper.SendResponse(pedidos);
            }
            catch (KeyNotFoundException ex)
            {
                return ResponseHelper.NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ResponseHelper.NotAuthorized(ex.Message);
            }
        }

        [HttpPost("{id}/pedido")]
        public async Task<IActionResult> CreatePedido(string id, [FromHeader(Name = "X-Mesa-Session")] string? sessionToken, [FromBody] CrearPedidoPublicoDTO dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                return ResponseHelper.NotAuthorized("Debes indicar una sesión pública válida.");

            try
            {
                var mesaId = await _mesaPublicSessionService.ResolveMesaPublicIdAsync(id, cancellationToken);
                var pedido = await _mesaPublicSessionService.CreatePedidoAsync(mesaId, sessionToken, dto, cancellationToken);
                return ResponseHelper.SendResponse(pedido, 201);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ResponseHelper.NotAuthorized(ex.Message);
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
    }
}
