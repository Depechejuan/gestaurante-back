using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    /// <summary>
    /// Gestiona las mesas del restaurante y su cierre operativo.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Administrador,Camarero")]
    public class MesaController : ControllerBase
    {
        private readonly MesaService _mesaService;
        private readonly FacturaService _facturaService;

        /// <summary>
        /// Inicializa el controlador con los servicios de mesas y facturación.
        /// </summary>
        public MesaController(MesaService mesaService, FacturaService facturaService)
        {
            _mesaService = mesaService;
            _facturaService = facturaService;
        }

        /// <summary>
        /// Obtiene el listado de mesas con su resumen operativo.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var mesas = await _mesaService.GetAllAsync(cancellationToken);
            return ResponseHelper.SendResponse(mesas);
        }

        /// <summary>
        /// Recupera el detalle completo de una mesa.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var mesa = await _mesaService.GetByIdAsync(id, cancellationToken);
            return mesa == null
                ? ResponseHelper.NotFound("Mesa no encontrada.")
                : ResponseHelper.SendResponse(mesa);
        }

        /// <summary>
        /// Crea una nueva mesa en el sistema.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearMesaDTO dto, CancellationToken cancellationToken)
        {
            var mesa = await _mesaService.CreateAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(mesa, 201);
        }

        /// <summary>
        /// Actualiza la configuración de una mesa existente.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EditarMesaDTO dto, CancellationToken cancellationToken)
        {
            var mesa = await _mesaService.UpdateAsync(id, dto, cancellationToken);
            return mesa == null
                ? ResponseHelper.NotFound("Mesa no encontrada.")
                : ResponseHelper.SendResponse(mesa);
        }

        /// <summary>
        /// Elimina una mesa siempre que no tenga dependencias activas.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var deleted = await _mesaService.DeleteAsync(id, cancellationToken);
            return deleted
                ? ResponseHelper.SendResponse(new { id, deleted = true })
                : ResponseHelper.NotFound("Mesa no encontrada.");
        }

        /// <summary>
        /// Cierra una mesa y genera la factura correspondiente de su consumo pendiente.
        /// </summary>
        [HttpPost("{id:guid}/cerrar")]
        public async Task<IActionResult> CloseMesa(Guid id, [FromBody] CerrarMesaDTO dto, CancellationToken cancellationToken)
        {
            var factura = await _facturaService.CloseMesaAsync(id, dto, cancellationToken);
            return ResponseHelper.SendResponse(factura, 201);
        }
    }
}
