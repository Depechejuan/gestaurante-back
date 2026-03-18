using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class PedidoController : ControllerBase
    {
        private readonly PedidoService _pedidoService;

        public PedidoController(PedidoService pedidoService)
        {
            _pedidoService = pedidoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var pedidos = await _pedidoService.GetAllAsync(cancellationToken);
            return ResponseHelper.SendResponse(pedidos);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var pedido = await _pedidoService.GetByIdAsync(id, cancellationToken);
            return pedido == null
                ? ResponseHelper.NotFound("Pedido no encontrado.")
                : ResponseHelper.SendResponse(pedido);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearPedidoDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var pedido = await _pedidoService.CreateAsync(dto, cancellationToken);
                return ResponseHelper.SendResponse(pedido, 201);
            }
            catch (KeyNotFoundException ex)
            {
                return ResponseHelper.NotFound(ex.Message);
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EditarPedidoDTO dto, CancellationToken cancellationToken)
        {
            var pedido = await _pedidoService.UpdateAsync(id, dto, cancellationToken);
            return pedido == null
                ? ResponseHelper.NotFound("Pedido no encontrado.")
                : ResponseHelper.SendResponse(pedido);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var deleted = await _pedidoService.DeleteAsync(id, cancellationToken);
            return deleted
                ? ResponseHelper.SendResponse(new { id, deleted = true })
                : ResponseHelper.NotFound("Pedido no encontrado.");
        }

        [HttpGet("{pedidoId:guid}/linea/{detalleId:guid}")]
        public async Task<IActionResult> GetDetalle(Guid pedidoId, Guid detalleId, CancellationToken cancellationToken)
        {
            var detalle = await _pedidoService.GetDetalleAsync(pedidoId, detalleId, cancellationToken);
            return detalle == null
                ? ResponseHelper.NotFound("Línea de pedido no encontrada.")
                : ResponseHelper.SendResponse(detalle);
        }

        [HttpPost("{pedidoId:guid}/linea")]
        public async Task<IActionResult> AddDetalle(Guid pedidoId, [FromBody] CrearDetallePedidoDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var detalle = await _pedidoService.AddDetalleAsync(pedidoId, dto, cancellationToken);
                return ResponseHelper.SendResponse(detalle, 201);
            }
            catch (KeyNotFoundException ex)
            {
                return ResponseHelper.NotFound(ex.Message);
            }
        }

        [HttpPut("{pedidoId:guid}/linea/{detalleId:guid}")]
        public async Task<IActionResult> UpdateDetalle(Guid pedidoId, Guid detalleId, [FromBody] EditarDetallePedidoDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var detalle = await _pedidoService.UpdateDetalleAsync(pedidoId, detalleId, dto, cancellationToken);
                return detalle == null
                    ? ResponseHelper.NotFound("Línea de pedido no encontrada.")
                    : ResponseHelper.SendResponse(detalle);
            }
            catch (KeyNotFoundException ex)
            {
                return ResponseHelper.NotFound(ex.Message);
            }
        }

        [HttpDelete("{pedidoId:guid}/linea/{detalleId:guid}")]
        public async Task<IActionResult> DeleteDetalle(Guid pedidoId, Guid detalleId, CancellationToken cancellationToken)
        {
            var deleted = await _pedidoService.DeleteDetalleAsync(pedidoId, detalleId, cancellationToken);
            return deleted
                ? ResponseHelper.SendResponse(new { id = detalleId, deleted = true })
                : ResponseHelper.NotFound("Línea de pedido no encontrada.");
        }
    }
}
