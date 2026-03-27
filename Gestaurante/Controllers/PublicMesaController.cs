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

        [HttpPost("{id:guid}/session")]
        public async Task<IActionResult> OpenSession(Guid id, [FromBody] OpenMesaPublicSessionDTO? dto, CancellationToken cancellationToken)
        {
            try
            {
                var session = await _mesaPublicSessionService.OpenOrResumeAsync(id, dto?.SessionToken, cancellationToken);
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

        [HttpGet("{id:guid}/pedidos")]
        public async Task<IActionResult> GetMyPedidos(Guid id, [FromHeader(Name = "X-Mesa-Session")] string? sessionToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                return ResponseHelper.NotAuthorized("Debes indicar una sesión pública válida.");

            try
            {
                var pedidos = await _mesaPublicSessionService.GetPedidosAsync(id, sessionToken, cancellationToken);
                return ResponseHelper.SendResponse(pedidos);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ResponseHelper.NotAuthorized(ex.Message);
            }
        }

        [HttpPost("{id:guid}/pedido")]
        public async Task<IActionResult> CreatePedido(Guid id, [FromHeader(Name = "X-Mesa-Session")] string? sessionToken, [FromBody] CrearPedidoPublicoDTO dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                return ResponseHelper.NotAuthorized("Debes indicar una sesión pública válida.");

            try
            {
                var pedido = await _mesaPublicSessionService.CreatePedidoAsync(id, sessionToken, dto, cancellationToken);
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
