using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class PedidoService
    {
        private readonly AppDbContext _db;

        public PedidoService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<PedidoDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var pedidos = await _db.Pedidos
                .AsNoTracking()
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync(cancellationToken);

            return await BuildPedidoListAsync(pedidos, cancellationToken);
        }

        public async Task<PedidoDTO?> GetByIdAsync(Guid pedidoId, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);

            if (pedido == null) return null;

            return await BuildPedidoDtoAsync(pedido, cancellationToken);
        }

        public async Task<PedidoDTO> CreateAsync(CrearPedidoDTO dto, CancellationToken cancellationToken = default)
        {
            var pedido = new Pedido(Guid.NewGuid(), DateTime.UtcNow, dto.Estado);
            await _db.Pedidos.AddAsync(pedido, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var detalleDto in dto.Detalles)
            {
                await CreateDetalleInternalAsync(pedido.IdPedido, detalleDto, cancellationToken);
            }

            return await GetByIdAsync(pedido.IdPedido, cancellationToken)
                ?? throw new InvalidOperationException("No se pudo recuperar el pedido recién creado.");
        }

        public async Task<PedidoDTO?> UpdateAsync(Guid pedidoId, EditarPedidoDTO dto, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);
            if (pedido == null) return null;

            if (dto.Estado.HasValue)
            {
                pedido.Estado = dto.Estado.Value;
            }

            SetFechaModificacion(pedido);
            await _db.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(pedidoId, cancellationToken);
        }

        public async Task<bool> DeleteAsync(Guid pedidoId, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);
            if (pedido == null) return false;

            var detalles = await _db.DetallesPedido.Where(d => d.IdPedido == pedidoId).ToListAsync(cancellationToken);
            if (detalles.Count > 0)
            {
                _db.DetallesPedido.RemoveRange(detalles);
            }

            _db.Pedidos.Remove(pedido);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<DetallePedidoDTO?> GetDetalleAsync(Guid pedidoId, Guid detalleId, CancellationToken cancellationToken = default)
        {
            var detalle = await _db.DetallesPedido
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.IdPedido == pedidoId && d.IdDetallePedido == detalleId, cancellationToken);

            if (detalle == null) return null;

            return await BuildDetalleDtoAsync(detalle, cancellationToken);
        }

        public async Task<DetallePedidoDTO> AddDetalleAsync(Guid pedidoId, CrearDetallePedidoDTO dto, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);
            if (pedido == null)
                throw new KeyNotFoundException("Pedido no encontrado.");

            var detalle = await CreateDetalleInternalAsync(pedidoId, dto, cancellationToken);
            SetFechaModificacion(pedido);
            await _db.SaveChangesAsync(cancellationToken);

            return await BuildDetalleDtoAsync(detalle, cancellationToken);
        }

        public async Task<DetallePedidoDTO?> UpdateDetalleAsync(Guid pedidoId, Guid detalleId, EditarDetallePedidoDTO dto, CancellationToken cancellationToken = default)
        {
            var detalle = await _db.DetallesPedido.FirstOrDefaultAsync(d => d.IdPedido == pedidoId && d.IdDetallePedido == detalleId, cancellationToken);
            if (detalle == null) return null;

            if (dto.Cantidad.HasValue)
            {
                detalle.Cantidad = dto.Cantidad.Value;
            }

            if (dto.IdPlato.HasValue && dto.IdPlato.Value != detalle.IdPlato)
            {
                var plato = await _db.Platos.FirstOrDefaultAsync(p => p.IdPlato == dto.IdPlato.Value, cancellationToken);
                if (plato == null)
                    throw new KeyNotFoundException("Plato no encontrado.");

                detalle.IdPlato = plato.IdPlato;
                detalle.PrecioUnitario = Convert.ToDouble(plato.Precio);
            }

            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);
            if (pedido != null)
            {
                SetFechaModificacion(pedido);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return await BuildDetalleDtoAsync(detalle, cancellationToken);
        }

        public async Task<bool> DeleteDetalleAsync(Guid pedidoId, Guid detalleId, CancellationToken cancellationToken = default)
        {
            var detalle = await _db.DetallesPedido.FirstOrDefaultAsync(d => d.IdPedido == pedidoId && d.IdDetallePedido == detalleId, cancellationToken);
            if (detalle == null) return false;

            _db.DetallesPedido.Remove(detalle);

            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);
            if (pedido != null)
            {
                SetFechaModificacion(pedido);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<DetallePedido> CreateDetalleInternalAsync(Guid pedidoId, CrearDetallePedidoDTO dto, CancellationToken cancellationToken)
        {
            var plato = await _db.Platos.FirstOrDefaultAsync(p => p.IdPlato == dto.IdPlato, cancellationToken);
            if (plato == null)
                throw new KeyNotFoundException("Plato no encontrado.");

            var detalle = new DetallePedido(Guid.NewGuid(), plato.IdPlato, pedidoId, dto.Cantidad, Convert.ToDouble(plato.Precio));
            await _db.DetallesPedido.AddAsync(detalle, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return detalle;
        }

        private async Task<List<PedidoDTO>> BuildPedidoListAsync(List<Pedido> pedidos, CancellationToken cancellationToken)
        {
            var pedidoIds = pedidos.Select(p => p.IdPedido).ToList();
            var detalles = await _db.DetallesPedido
                .AsNoTracking()
                .Where(d => pedidoIds.Contains(d.IdPedido))
                .ToListAsync(cancellationToken);

            var platoIds = detalles.Select(d => d.IdPlato).Distinct().ToList();
            var platos = await _db.Platos
                .AsNoTracking()
                .Where(p => platoIds.Contains(p.IdPlato))
                .ToDictionaryAsync(p => p.IdPlato, p => p.Nombre, cancellationToken);

            return pedidos.Select(pedido =>
            {
                var detallesPedido = detalles
                    .Where(d => d.IdPedido == pedido.IdPedido)
                    .Select(detalle => MapDetalle(detalle, platos.GetValueOrDefault(detalle.IdPlato, "Plato desconocido")))
                    .ToList();

                return new PedidoDTO
                {
                    IdPedido = pedido.IdPedido,
                    FechaPedido = pedido.FechaPedido,
                    FechaModificacion = pedido.FechaModificacion,
                    Estado = pedido.Estado,
                    Detalles = detallesPedido,
                    Total = detallesPedido.Sum(d => d.Subtotal)
                };
            }).ToList();
        }

        private async Task<PedidoDTO> BuildPedidoDtoAsync(Pedido pedido, CancellationToken cancellationToken)
        {
            var detalles = await _db.DetallesPedido
                .AsNoTracking()
                .Where(d => d.IdPedido == pedido.IdPedido)
                .ToListAsync(cancellationToken);

            var platoIds = detalles.Select(d => d.IdPlato).Distinct().ToList();
            var platos = await _db.Platos
                .AsNoTracking()
                .Where(p => platoIds.Contains(p.IdPlato))
                .ToDictionaryAsync(p => p.IdPlato, p => p.Nombre, cancellationToken);

            var detallesDto = detalles
                .Select(detalle => MapDetalle(detalle, platos.GetValueOrDefault(detalle.IdPlato, "Plato desconocido")))
                .ToList();

            return new PedidoDTO
            {
                IdPedido = pedido.IdPedido,
                FechaPedido = pedido.FechaPedido,
                FechaModificacion = pedido.FechaModificacion,
                Estado = pedido.Estado,
                Detalles = detallesDto,
                Total = detallesDto.Sum(d => d.Subtotal)
            };
        }

        private async Task<DetallePedidoDTO> BuildDetalleDtoAsync(DetallePedido detalle, CancellationToken cancellationToken)
        {
            var platoNombre = await _db.Platos
                .AsNoTracking()
                .Where(p => p.IdPlato == detalle.IdPlato)
                .Select(p => p.Nombre)
                .FirstOrDefaultAsync(cancellationToken) ?? "Plato desconocido";

            return MapDetalle(detalle, platoNombre);
        }

        private static DetallePedidoDTO MapDetalle(DetallePedido detalle, string platoNombre)
        {
            return new DetallePedidoDTO
            {
                IdDetallePedido = detalle.IdDetallePedido,
                IdPedido = detalle.IdPedido,
                IdPlato = detalle.IdPlato,
                PlatoNombre = platoNombre,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario,
                Subtotal = detalle.Cantidad * detalle.PrecioUnitario
            };
        }

        private static void SetFechaModificacion(Pedido pedido)
        {
            var property = typeof(Pedido).GetProperty(nameof(Pedido.FechaModificacion));
            property?.SetValue(pedido, DateTime.UtcNow);
        }
    }
}
