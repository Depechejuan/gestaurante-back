namespace Gestaurante.Models.DTO
{
    public class MesaDTO
    {
        public Guid IdMesa { get; set; }
        public int Capacidad { get; set; }
        public bool Estado { get; set; }
        public string Ubicacion { get; set; } = string.Empty;
    }
}
