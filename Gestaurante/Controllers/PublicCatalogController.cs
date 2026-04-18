using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    /// <summary>
    /// Expone el catálogo público de platos disponible para carta, QR y pedido online.
    /// </summary>
    [ApiController]
    [Route("public/catalogo")]
    public class PublicCatalogController : ControllerBase
    {
        private readonly PublicCheckoutService _publicCheckoutService;

        /// <summary>
        /// Inicializa el controlador con el servicio de catálogo público.
        /// </summary>
        public PublicCatalogController(PublicCheckoutService publicCheckoutService)
        {
            _publicCheckoutService = publicCheckoutService;
        }

        /// <summary>
        /// Devuelve el catálogo público completo de platos disponibles.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCatalogo(CancellationToken cancellationToken)
        {
            return ResponseHelper.SendResponse(await _publicCheckoutService.GetCatalogoAsync(cancellationToken));
        }

        /// <summary>
        /// Devuelve el detalle público de un plato concreto.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPlato(Guid id, CancellationToken cancellationToken)
        {
            var plato = await _publicCheckoutService.GetCatalogoItemAsync(id, cancellationToken);
            if (plato == null)
                return ResponseHelper.NotFound("Plato no encontrado.");

            return ResponseHelper.SendResponse(plato);
        }
    }
}
