namespace Gestaurante.Models.DTO
{
    public class ClienteRegisterResponseDTO
    {
        public Guid IdUsuarioCliente { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool EmailVerificado { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
