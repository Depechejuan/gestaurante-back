using System.Security.Claims;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    /// <summary>
    /// Gestiona la cuenta pública del cliente: registro, perfil, direcciones y métodos de pago.
    /// </summary>
    [ApiController]
    [Route("public/account")]
    public class PublicAccountController : ControllerBase
    {
        private readonly CustomerAccountService _customerAccountService;

        /// <summary>
        /// Inicializa el controlador con el servicio de cuentas de cliente.
        /// </summary>
        public PublicAccountController(CustomerAccountService customerAccountService)
        {
            _customerAccountService = customerAccountService;
        }

        /// <summary>
        /// Registra una nueva cuenta de cliente.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] ClienteRegisterDTO dto, CancellationToken cancellationToken)
        {
            var result = await _customerAccountService.RegisterAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(result, 201);
        }

        /// <summary>
        /// Verifica el email del cliente mediante el código enviado.
        /// </summary>
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] ClienteVerifyEmailDTO dto, CancellationToken cancellationToken)
        {
            await _customerAccountService.VerifyEmailAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(new { verified = true });
        }

        /// <summary>
        /// Reenvía un nuevo código de validación al correo del cliente.
        /// </summary>
        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendCode([FromBody] ClienteResendCodeDTO dto, CancellationToken cancellationToken)
        {
            await _customerAccountService.ResendVerificationCodeAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(new { sent = true });
        }

        /// <summary>
        /// Inicia sesión como cliente y devuelve el token público.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] ClienteLoginDTO dto, CancellationToken cancellationToken)
        {
            var token = await _customerAccountService.LoginAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(token, 201);
        }

        /// <summary>
        /// Recupera el perfil del cliente autenticado.
        /// </summary>
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

        /// <summary>
        /// Actualiza los datos personales y fiscales del cliente autenticado.
        /// </summary>
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

        /// <summary>
        /// Devuelve todas las direcciones del cliente autenticado.
        /// </summary>
        [Authorize(AuthenticationSchemes = "CustomerBearer")]
        [HttpGet("addresses")]
        public async Task<IActionResult> GetAddresses(CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");
            

            return ResponseHelper.SendResponse(await _customerAccountService.GetDireccionesAsync(clienteId.Value, cancellationToken));
        }

        /// <summary>
        /// Crea una nueva dirección para el cliente autenticado.
        /// </summary>
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

        /// <summary>
        /// Actualiza una dirección existente del cliente autenticado.
        /// </summary>
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

        /// <summary>
        /// Elimina una dirección del cliente autenticado.
        /// </summary>
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

        /// <summary>
        /// Devuelve los métodos de pago guardados por el cliente autenticado.
        /// </summary>
        [Authorize(AuthenticationSchemes = "CustomerBearer")]
        [HttpGet("payment-methods")]
        public async Task<IActionResult> GetPaymentMethods(CancellationToken cancellationToken)
        {
            var clienteId = GetCustomerId();
            if (!clienteId.HasValue)
                return ResponseHelper.NotAuthorized("Token de cliente inválido.");

            return ResponseHelper.SendResponse(await _customerAccountService.GetMetodosPagoAsync(clienteId.Value, cancellationToken));
        }

        /// <summary>
        /// Crea un nuevo método de pago para el cliente autenticado.
        /// </summary>
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

        /// <summary>
        /// Elimina un método de pago del cliente autenticado.
        /// </summary>
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

        /// <summary>
        /// Obtiene el identificador del cliente autenticado desde el token público.
        /// </summary>
        private Guid? GetCustomerId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            return Guid.TryParse(value, out var guid) ? guid : null;
        }
    }
}
