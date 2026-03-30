namespace Gestaurante.Models.DTO
{
    public class FacturaLineaDTO
    {
        public Guid IdDetallePedido { get; set; }
        public Guid IdPedido { get; set; }
        public Guid IdPlato { get; set; }
        public string PlatoNombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public double PrecioUnitario { get; set; }
        public double TotalLinea { get; set; }
    }
}
