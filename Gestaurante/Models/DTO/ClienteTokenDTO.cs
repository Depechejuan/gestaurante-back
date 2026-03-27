namespace Gestaurante.Models.DTO
{
    public class ClienteTokenDTO
    {
        public string Token { get; set; }
        public DateTime ExpiraEn { get; set; }
        public Guid IdUsuarioCliente { get; set; }
        public string Email { get; set; }
        public bool EmailVerificado { get; set; }

        public ClienteTokenDTO(string token, DateTime expiraEn, Guid idUsuarioCliente, string email, bool emailVerificado)
        {
            Token = token;
            ExpiraEn = expiraEn;
            IdUsuarioCliente = idUsuarioCliente;
            Email = email;
            EmailVerificado = emailVerificado;
        }
    }
}
