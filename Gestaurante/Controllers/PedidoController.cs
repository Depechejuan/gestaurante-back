using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    /// <summary>
    /// Gestiona pedidos internos, sus líneas y las transiciones operativas de sala, cocina y reparto.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Administrador,Camarero,Cocinero,Repartidor")]
    public class PedidoController : ControllerBase
    {
        private readonly PedidoService _pedidoService;

        /// <summary>
        /// Inicializa el controlador con el servicio de pedidos.
        /// </summary>
        public PedidoController(PedidoService pedidoService)
        {
            _pedidoService = pedidoService;
        }

        /// <summary>
        /// Devuelve todos los pedidos disponibles para la operativa interna.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var pedidos = await _pedidoService.GetAllAsync(cancellationToken);
            return ResponseHelper.SendResponse(pedidos);
        }

        /// <summary>
        /// Recupera el detalle completo de un pedido.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var pedido = await _pedidoService.GetByIdAsync(id, cancellationToken);
            return pedido == null
                ? ResponseHelper.NotFound("Pedido no encontrado.")
                : ResponseHelper.SendResponse(pedido);
        }

        /// <summary>
        /// Crea un nuevo pedido interno.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearPedidoDTO dto, CancellationToken cancellationToken)
        {
            var pedido = await _pedidoService.CreateAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(pedido, 201);
        }

        /// <summary>
        /// Actualiza el estado y los datos editables de un pedido.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EditarPedidoDTO dto, CancellationToken cancellationToken)
        {
            var pedido = await _pedidoService.UpdateAsync(id, dto, cancellationToken);
            return pedido == null
                ? ResponseHelper.NotFound("Pedido no encontrado.")
                : ResponseHelper.SendResponse(pedido);
        }

        /// <summary>
        /// Elimina un pedido cuando la operación lo permite.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var deleted = await _pedidoService.DeleteAsync(id, cancellationToken);
            return deleted
                ? ResponseHelper.SendResponse(new { id, deleted = true })
                : ResponseHelper.NotFound("Pedido no encontrado.");
        }

        /// <summary>
        /// Cancela un pedido completo conservando la trazabilidad.
        /// </summary>
        [HttpPost("{id:guid}/cancelar")]
        [Authorize(Roles = "Administrador,Camarero")]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelarPedidoDTO dto, CancellationToken cancellationToken)
        {
            var pedido = await _pedidoService.CancelAsync(id, dto, cancellationToken);
            return pedido == null
                ? ResponseHelper.NotFound("Pedido no encontrado.")
                : ResponseHelper.SendResponse(pedido);
        }

        /// <summary>
        /// Recupera una línea concreta de un pedido.
        /// </summary>
        [HttpGet("{pedidoId:guid}/linea/{detalleId:guid}")]
        public async Task<IActionResult> GetDetalle(Guid pedidoId, Guid detalleId, CancellationToken cancellationToken)
        {
            var detalle = await _pedidoService.GetDetalleAsync(pedidoId, detalleId, cancellationToken);
            return detalle == null
                ? ResponseHelper.NotFound("Línea de pedido no encontrada.")
                : ResponseHelper.SendResponse(detalle);
        }

        /// <summary>
        /// Añade una nueva línea a un pedido existente.
        /// </summary>
        [HttpPost("{pedidoId:guid}/linea")]
        public async Task<IActionResult> AddDetalle(Guid pedidoId, [FromBody] CrearDetallePedidoDTO dto, CancellationToken cancellationToken)
        {
            var detalle = await _pedidoService.AddDetalleAsync(pedidoId, dto, cancellationToken);
            return ResponseHelper.SendResponse(detalle, 201);
        }

        /// <summary>
        /// Actualiza una línea de pedido o su estado operativo.
        /// </summary>
        [HttpPut("{pedidoId:guid}/linea/{detalleId:guid}")]
        public async Task<IActionResult> UpdateDetalle(Guid pedidoId, Guid detalleId, [FromBody] EditarDetallePedidoDTO dto, CancellationToken cancellationToken)
        {
            if (dto.Estado.HasValue && !CanManageDetalleEstado(dto.Estado.Value))
                return ResponseHelper.Forbidden("No tienes permisos para cambiar esta línea a ese estado.");

            var detalle = await _pedidoService.UpdateDetalleAsync(pedidoId, detalleId, dto, cancellationToken);
            return detalle == null
                ? ResponseHelper.NotFound("Línea de pedido no encontrada.")
                : ResponseHelper.SendResponse(detalle);
        }

        /// <summary>
        /// Elimina una línea de pedido cuando la operación lo permite.
        /// </summary>
        [HttpDelete("{pedidoId:guid}/linea/{detalleId:guid}")]
        public async Task<IActionResult> DeleteDetalle(Guid pedidoId, Guid detalleId, CancellationToken cancellationToken)
        {
            var deleted = await _pedidoService.DeleteDetalleAsync(pedidoId, detalleId, cancellationToken);
            return deleted
                ? ResponseHelper.SendResponse(new { id = detalleId, deleted = true })
                : ResponseHelper.NotFound("Línea de pedido no encontrada.");
        }

        /// <summary>
        /// Cancela una línea concreta manteniendo el histórico del pedido.
        /// </summary>
        [HttpPost("{pedidoId:guid}/linea/{detalleId:guid}/cancelar")]
        [Authorize(Roles = "Administrador,Camarero")]
        public async Task<IActionResult> CancelDetalle(Guid pedidoId, Guid detalleId, [FromBody] CancelarDetallePedidoDTO dto, CancellationToken cancellationToken)
        {
            var detalle = await _pedidoService.CancelDetalleAsync(pedidoId, detalleId, dto, cancellationToken);
            return detalle == null
                ? ResponseHelper.NotFound("Línea de pedido no encontrada.")
                : ResponseHelper.SendResponse(detalle);
        }

        /// <summary>
        /// Comprueba si el rol autenticado puede aplicar una transición concreta de estado a una línea.
        /// </summary>
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
