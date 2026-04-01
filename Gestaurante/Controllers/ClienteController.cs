using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Administrador,Camarero")]
    public class ClienteController : ControllerBase
    {
        private readonly CustomerAccountService _customerAccountService;

        public ClienteController(CustomerAccountService customerAccountService)
        {
            _customerAccountService = customerAccountService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? query, CancellationToken cancellationToken)
        {
            return ResponseHelper.SendResponse(await _customerAccountService.GetInternalClientesAsync(query, cancellationToken));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var cliente = await _customerAccountService.GetProfileAsync(id, cancellationToken);
            return cliente == null
                ? ResponseHelper.NotFound("Cliente no encontrado.")
                : ResponseHelper.SendResponse(cliente);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInternalClienteDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var cliente = await _customerAccountService.CreateInternalClienteAsync(dto, cancellationToken);
                return ResponseHelper.SendResponse(cliente, 201);
            }
            catch (InvalidOperationException ex)
            {
                return ResponseHelper.Conflict(ex.Message);
            }
        }
    }
}
