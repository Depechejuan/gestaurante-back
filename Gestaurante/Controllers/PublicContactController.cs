using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("public/contact")]
    public class PublicContactController : ControllerBase
    {
        private readonly ContactService _contactService;

        public PublicContactController(ContactService contactService)
        {
            _contactService = contactService;
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] ContactMessageDTO dto, CancellationToken cancellationToken)
        {
            await _contactService.SendAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(new { sent = true }, 201);
        }
    }
}
