using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("public/catalogo")]
    public class PublicCatalogController : ControllerBase
    {
        private readonly PublicCheckoutService _publicCheckoutService;

        public PublicCatalogController(PublicCheckoutService publicCheckoutService)
        {
            _publicCheckoutService = publicCheckoutService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCatalogo(CancellationToken cancellationToken)
        {
            return ResponseHelper.SendResponse(await _publicCheckoutService.GetCatalogoAsync(cancellationToken));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPlato(Guid id, CancellationToken cancellationToken)
        {
            var plato = await _publicCheckoutService.GetCatalogoItemAsync(id, cancellationToken);
            if (plato == null)
                return NotFound(new { error = "Plato no encontrado." });

            return ResponseHelper.SendResponse(plato);
        }
    }
}
