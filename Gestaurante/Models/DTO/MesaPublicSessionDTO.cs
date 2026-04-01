namespace Gestaurante.Models.DTO
{
    public class MesaPublicSessionDTO
    {
        public Guid IdMesa { get; set; }
        public string SessionToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool CanOrder { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
