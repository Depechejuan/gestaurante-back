namespace Gestaurante.Models.DTO
{
    public class MesaDetalleDTO : MesaDTO
    {
        public List<PedidoDTO> Pedidos { get; set; } = new();
    }
}
