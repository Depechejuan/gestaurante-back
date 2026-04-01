using System.Security.Claims;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("public/account")]
    public class PublicAccountController : ControllerBase
    {
        private readonly CustomerAccountService _customerAccountService;

        public PublicAccountController(CustomerAccountService customerAccountService)
        {
            _customerAccountService = customerAccountService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] ClienteRegisterDTO dto, CancellationToken cancellationToken)
        {
            var result = await _customerAccountService.RegisterAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(result, 201);
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] ClienteVerifyEmailDTO dto, CancellationToken cancellationToken)
        {
            await _customerAccountService.VerifyEmailAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(new { verified = true });
        }

        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendCode([FromBody] ClienteResendCodeDTO dto, CancellationToken cancellationToken)
        {
            await _customerAccountService.ResendVerificationCodeAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(new { sent = true });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] ClienteLoginDTO dto, CancellationToken cancellationToken)
        {
            var token = await _customerAccountService.LoginAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(token, 201);
        }

        [Authorize(AuthenticationSchemes = "CustomerBearer")]
        [HttpGet("me")]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            var profile = await _customerAccountService.GetProfileAsync(clienteId.Value, cancellationToken);
            return profile == null ? ResponseHelper.NotFound("Cliente no encontrado.") : ResponseHelper.SendResponse(profile);
        }

        [Authorize(AuthenticationSchemes = "CustomerBearer")]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateClienteProfileDTO dto, CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            var profile = await _customerAccountService.UpdateProfileAsync(clienteId.Value, dto, cancellationToken);
            return profile == null ? ResponseHelper.NotFound("Cliente no encontrado.") : ResponseHelper.SendResponse(profile);
        }

        [Authorize(AuthenticationSchemes = "CustomerBearer")]
        [HttpGet("addresses")]
        public async Task<IActionResult> GetAddresses(CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");
            

            return ResponseHelper.SendResponse(await _customerAccountService.GetDireccionesAsync(clienteId.Value, cancellationToken));
        }

        [Authorize(AuthenticationSchemes = "CustomerBearer")]
        [HttpPost("addresses")]
        public async Task<IActionResult> CreateAddress([FromBody] CreateClienteDireccionDTO dto, CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            var address = await _customerAccountService.CreateDireccionAsync(clienteId.Value, dto, cancellationToken);
            return ResponseHelper.SendResponse(address, 201);
        }

        [Authorize(AuthenticationSchemes = "CustomerBearer")]
        [HttpPut("addresses/{id:guid}")]
        public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateClienteDireccionDTO dto, CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            var address = await _customerAccountService.UpdateDireccionAsync(clienteId.Value, id, dto, cancellationToken);
            return address == null ? ResponseHelper.NotFound("Dirección no encontrada.") : ResponseHelper.SendResponse(address);
        }

        [Authorize(AuthenticationSchemes = "CustomerBearer")]
        [HttpDelete("addresses/{id:guid}")]
        public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            await _customerAccountService.DeleteDireccionAsync(clienteId.Value, id, cancellationToken);
            return ResponseHelper.SendResponse(new { deleted = true });
        }

        [Authorize(AuthenticationSchemes = "CustomerBearer")]
        [HttpGet("payment-methods")]
        public async Task<IActionResult> GetPaymentMethods(CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            return ResponseHelper.SendResponse(await _customerAccountService.GetMetodosPagoAsync(clienteId.Value, cancellationToken));
        }

        [Authorize(AuthenticationSchemes = "CustomerBearer")]
        [HttpPost("payment-methods")]
        public async Task<IActionResult> CreatePaymentMethod([FromBody] CreateClienteMetodoPagoDTO dto, CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            var paymentMethod = await _customerAccountService.CreateMetodoPagoAsync(clienteId.Value, dto, cancellationToken);
            return ResponseHelper.SendResponse(paymentMethod, 201);
        }

        [Authorize(AuthenticationSchemes = "CustomerBearer")]
        [HttpDelete("payment-methods/{id:guid}")]
        public async Task<IActionResult> DeletePaymentMethod(Guid id, CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            await _customerAccountService.DeleteMetodoPagoAsync(clienteId.Value, id, cancellationToken);
            return ResponseHelper.SendResponse(new { deleted = true });
        }

        private Guid? GetCustomerId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            return Guid.TryParse(value, out var guid) ? guid : null;
        }
    }
}
