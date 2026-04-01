using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Administrador,Camarero,Cocinero,Repartidor")]
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
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.ValidationError(ex.Message);
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EditarPedidoDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var pedido = await _pedidoService.UpdateAsync(id, dto, cancellationToken);
                return pedido == null
                    ? ResponseHelper.NotFound("Pedido no encontrado.")
                    : ResponseHelper.SendResponse(pedido);
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
                var deleted = await _pedidoService.DeleteAsync(id, cancellationToken);
                return deleted
                    ? ResponseHelper.SendResponse(new { id, deleted = true })
                    : ResponseHelper.NotFound("Pedido no encontrado.");
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.Conflict(ex.Message);
            }
        }

        [HttpPost("{id:guid}/cancelar")]
        [Authorize(Roles = "Administrador,Camarero")]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelarPedidoDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var pedido = await _pedidoService.CancelAsync(id, dto, cancellationToken);
                return pedido == null
                    ? ResponseHelper.NotFound("Pedido no encontrado.")
                    : ResponseHelper.SendResponse(pedido);
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.Conflict(ex.Message);
            }
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
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.Conflict(ex.Message);
            }
        }

        [HttpPut("{pedidoId:guid}/linea/{detalleId:guid}")]
        public async Task<IActionResult> UpdateDetalle(Guid pedidoId, Guid detalleId, [FromBody] EditarDetallePedidoDTO dto, CancellationToken cancellationToken)
        {
            if (dto.Estado.HasValue && !CanManageDetalleEstado(dto.Estado.Value))
                return ResponseHelper.Forbidden("No tienes permisos para cambiar esta línea a ese estado.");

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
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.Conflict(ex.Message);
            }
        }

        [HttpDelete("{pedidoId:guid}/linea/{detalleId:guid}")]
        public async Task<IActionResult> DeleteDetalle(Guid pedidoId, Guid detalleId, CancellationToken cancellationToken)
        {
            try
            {
                var deleted = await _pedidoService.DeleteDetalleAsync(pedidoId, detalleId, cancellationToken);
                return deleted
                    ? ResponseHelper.SendResponse(new { id = detalleId, deleted = true })
                    : ResponseHelper.NotFound("Línea de pedido no encontrada.");
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.Conflict(ex.Message);
            }
        }

        [HttpPost("{pedidoId:guid}/linea/{detalleId:guid}/cancelar")]
        [Authorize(Roles = "Administrador,Camarero")]
        public async Task<IActionResult> CancelDetalle(Guid pedidoId, Guid detalleId, [FromBody] CancelarDetallePedidoDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var detalle = await _pedidoService.CancelDetalleAsync(pedidoId, detalleId, dto, cancellationToken);
                return detalle == null
                    ? ResponseHelper.NotFound("Línea de pedido no encontrada.")
                    : ResponseHelper.SendResponse(detalle);
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.Conflict(ex.Message);
            }
        }

        private bool CanManageDetalleEstado(EstadoDetallePedido estado)
        {
            if (User.IsInRole("Administrador"))
                return true;

            return estado switch
            {
                EstadoDetallePedido.EN_COCINA => User.IsInRole("Camarero"),
                EstadoDetallePedido.ENTREGADA => User.IsInRole("Camarero"),
                EstadoDetallePedido.PREPARADO => User.IsInRole("Cocinero"),
                _ => false
            };
        }
    }
}
