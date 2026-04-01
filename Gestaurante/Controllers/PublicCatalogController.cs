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
    }
}
