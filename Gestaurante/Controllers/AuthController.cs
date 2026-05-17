using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    /// <summary>
    /// Expone acciones públicas de recuperación de acceso comunes a empleados y clientes.
    /// </summary>
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AccountActionTokenService _accountActionTokenService;

        /// <summary>
        /// Inicializa el controlador con el servicio de acciones de cuenta.
        /// </summary>
        public AuthController(AccountActionTokenService accountActionTokenService)
        {
            _accountActionTokenService = accountActionTokenService;
        }

        /// <summary>
        /// Solicita un enlace de recuperación de contraseña para el email indicado.
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO dto, CancellationToken cancellationToken)
        {
            await _accountActionTokenService.RequestPasswordResetAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(new { sent = true });
        }

        /// <summary>
        /// Cambia la contraseña usando un enlace de recuperación válido.
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto, CancellationToken cancellationToken)
        {
            await _accountActionTokenService.ResetPasswordAsync(dto, cancellationToken);
            return ResponseHelper.SendResponse(new { reset = true });
        }
    }
}
