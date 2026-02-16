namespace Gestaurante.Models.DTO
{
    public class TokenDTO
    {
        public string Token { get; set; }
        public DateTime ExpiraEn { get; set; }
        public Guid Id { get; set; }
        public TipoEmpleado Tipo { get; set; }

        public TokenDTO(string token, DateTime expire, Guid id, TipoEmpleado tipo)
        {
            this.Token = token;
            this.ExpiraEn = expire;
            this.Id = id;
            this.Tipo = tipo;
        }
    }
}
