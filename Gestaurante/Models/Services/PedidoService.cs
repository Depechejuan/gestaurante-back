using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class PedidoService
    {
        private const string PedidoBloqueadoMessage = "El pedido ya fue enviado y no admite cambios estructurales.";

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

            if (pedido == null)
            {
                return null;
            }

            return await BuildPedidoDtoAsync(pedido, cancellationToken);
        }

        public async Task<PedidoDTO> CreateAsync(CrearPedidoDTO dto, CancellationToken cancellationToken = default)
        {
            return await CreateAsync(dto, null, cancellationToken);
        }

        public async Task<PedidoDTO> CreateAsync(CrearPedidoDTO dto, Guid? mesaPublicSessionId, CancellationToken cancellationToken = default)
        {
            if (dto.Detalles.Count == 0)
            {
                throw new InvalidOperationException("El pedido debe contener al menos una línea.");
            }

            var mesa = await _db.Mesas.FirstOrDefaultAsync(m => m.IdMesa == dto.IdMesa, cancellationToken);
            if (mesa == null)
            {
                throw new KeyNotFoundException("Mesa no encontrada.");
            }

            var pedido = new Pedido(Guid.NewGuid(), dto.IdMesa, DateTime.UtcNow, dto.Estado, mesaPublicSessionId);
            await _db.Pedidos.AddAsync(pedido, cancellationToken);

            foreach (var detalleDto in dto.Detalles)
            {
                var detalle = await CreateDetalleInternalAsync(pedido.IdPedido, detalleDto, cancellationToken);
                pedido.DetallesPedido.Add(detalle);
            }

            mesa.Estado = false;
            await _db.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(pedido.IdPedido, cancellationToken)
                ?? throw new InvalidOperationException("No se pudo recuperar el pedido recién creado.");
        }

        public async Task<List<PedidoDTO>> GetByMesaPublicSessionAsync(Guid mesaId, Guid mesaPublicSessionId, CancellationToken cancellationToken = default)
        {
            var pedidos = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdMesa == mesaId && p.IdMesaPublicSession == mesaPublicSessionId)
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync(cancellationToken);

            return await BuildPedidoListAsync(pedidos, cancellationToken);
        }

        public async Task<PedidoDTO?> UpdateAsync(Guid pedidoId, EditarPedidoDTO dto, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);
            if (pedido == null)
            {
                return null;
            }

            if (pedido.IdFactura.HasValue)
            {
                throw new InvalidOperationException("No se puede modificar el estado de un pedido ya facturado.");
            }

            if (!dto.Estado.HasValue || dto.Estado.Value == pedido.Estado)
            {
                return await GetByIdAsync(pedidoId, cancellationToken);
            }

            ValidateEstadoTransition(pedido.Estado, dto.Estado.Value);

            pedido.Estado = dto.Estado.Value;
            SetFechaModificacion(pedido);
            await _db.SaveChangesAsync(cancellationToken);

            await UpdateMesaAvailabilityAsync(pedido.IdMesa, cancellationToken);
            return await GetByIdAsync(pedidoId, cancellationToken);
        }

        public async Task<bool> DeleteAsync(Guid pedidoId, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);
            if (pedido == null)
            {
                return false;
            }

            if (pedido.IdFactura.HasValue)
            {
                throw new InvalidOperationException("No se puede borrar un pedido ya facturado.");
            }

            var detalles = await _db.DetallesPedido.Where(d => d.IdPedido == pedidoId).ToListAsync(cancellationToken);
            if (detalles.Count > 0)
            {
                _db.DetallesPedido.RemoveRange(detalles);
            }

            var mesaId = pedido.IdMesa;
            _db.Pedidos.Remove(pedido);
            await _db.SaveChangesAsync(cancellationToken);
            await UpdateMesaAvailabilityAsync(mesaId, cancellationToken);
            return true;
        }

        public async Task<PedidoDTO?> CancelAsync(Guid pedidoId, CancelarPedidoDTO? dto, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos
                .Include(p => p.DetallesPedido)
                .FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);

            if (pedido == null)
            {
                return null;
            }

            if (pedido.IdFactura.HasValue)
            {
                throw new InvalidOperationException("No se puede cancelar un pedido ya facturado.");
            }

            if (pedido.Estado != EstadoPedido.CANCELADO)
            {
                pedido.Estado = EstadoPedido.CANCELADO;
                foreach (var detalle in pedido.DetallesPedido.Where(d => d.Estado == EstadoDetallePedido.ACTIVA))
                {
                    detalle.Estado = EstadoDetallePedido.CANCELADA;
                    detalle.FechaCancelacion = DateTime.UtcNow;
                }

                SetFechaModificacion(pedido);
                await _db.SaveChangesAsync(cancellationToken);
                await UpdateMesaAvailabilityAsync(pedido.IdMesa, cancellationToken);
            }

            return await GetByIdAsync(pedidoId, cancellationToken);
        }

        public async Task<DetallePedidoDTO?> GetDetalleAsync(Guid pedidoId, Guid detalleId, CancellationToken cancellationToken = default)
        {
            var detalle = await _db.DetallesPedido
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.IdPedido == pedidoId && d.IdDetallePedido == detalleId, cancellationToken);

            if (detalle == null)
            {
                return null;
            }

            return await BuildDetalleDtoAsync(detalle, cancellationToken);
        }

        public async Task<DetallePedidoDTO> AddDetalleAsync(Guid pedidoId, CrearDetallePedidoDTO dto, CancellationToken cancellationToken = default)
        {
            await EnsurePedidoExistsAsync(pedidoId, cancellationToken);
            throw new InvalidOperationException(PedidoBloqueadoMessage);
        }

        public async Task<DetallePedidoDTO?> UpdateDetalleAsync(Guid pedidoId, Guid detalleId, EditarDetallePedidoDTO dto, CancellationToken cancellationToken = default)
        {
            var detalle = await _db.DetallesPedido
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.IdPedido == pedidoId && d.IdDetallePedido == detalleId, cancellationToken);

            if (detalle == null)
            {
                return null;
            }

            throw new InvalidOperationException(PedidoBloqueadoMessage);
        }

        public async Task<bool> DeleteDetalleAsync(Guid pedidoId, Guid detalleId, CancellationToken cancellationToken = default)
        {
            var detalle = await _db.DetallesPedido
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.IdPedido == pedidoId && d.IdDetallePedido == detalleId, cancellationToken);

            if (detalle == null)
            {
                return false;
            }

            throw new InvalidOperationException(PedidoBloqueadoMessage);
        }

        public async Task<DetallePedidoDTO?> CancelDetalleAsync(Guid pedidoId, Guid detalleId, CancelarDetallePedidoDTO? dto, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos
                .Include(p => p.DetallesPedido)
                .FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);

            if (pedido == null)
            {
                return null;
            }

            if (pedido.IdFactura.HasValue)
            {
                throw new InvalidOperationException("No se puede cancelar una línea de un pedido ya facturado.");
            }

            var detalle = pedido.DetallesPedido.FirstOrDefault(d => d.IdDetallePedido == detalleId);
            if (detalle == null)
            {
                return null;
            }

            if (detalle.Estado == EstadoDetallePedido.ACTIVA)
            {
                detalle.Estado = EstadoDetallePedido.CANCELADA;
                detalle.FechaCancelacion = DateTime.UtcNow;
                SetFechaModificacion(pedido);

                if (!pedido.DetallesPedido.Any(d => d.Estado == EstadoDetallePedido.ACTIVA))
                {
                    pedido.Estado = EstadoPedido.CANCELADO;
                }

                await _db.SaveChangesAsync(cancellationToken);
                await UpdateMesaAvailabilityAsync(pedido.IdMesa, cancellationToken);
            }

            return await BuildDetalleDtoAsync(detalle, cancellationToken);
        }

        private async Task EnsurePedidoExistsAsync(Guid pedidoId, CancellationToken cancellationToken)
        {
            var exists = await _db.Pedidos.AnyAsync(p => p.IdPedido == pedidoId, cancellationToken);
            if (!exists)
            {
                throw new KeyNotFoundException("Pedido no encontrado.");
            }
        }

        private async Task<DetallePedido> CreateDetalleInternalAsync(Guid pedidoId, CrearDetallePedidoDTO dto, CancellationToken cancellationToken)
        {
            var plato = await _db.Platos.FirstOrDefaultAsync(p => p.IdPlato == dto.IdPlato, cancellationToken);
            if (plato == null)
            {
                throw new KeyNotFoundException("Plato no encontrado.");
            }

            return new DetallePedido(Guid.NewGuid(), plato.IdPlato, pedidoId, dto.Cantidad, Convert.ToDouble(plato.Precio));
        }

        private async Task<List<PedidoDTO>> BuildPedidoListAsync(List<Pedido> pedidos, CancellationToken cancellationToken)
        {
            var pedidoIds = pedidos.Select(p => p.IdPedido).ToList();
            var detalles = await _db.DetallesPedido
                .AsNoTracking()
                .Where(d => pedidoIds.Contains(d.IdPedido))
                .ToListAsync(cancellationToken);

            var platos = await ResolvePlatoNamesAsync(detalles, cancellationToken);

            return pedidos.Select(pedido =>
            {
                var detallesPedido = detalles
                    .Where(d => d.IdPedido == pedido.IdPedido)
                    .Select(detalle => MapDetalle(detalle, platos.GetValueOrDefault(detalle.IdPlato, "Plato desconocido")))
                    .ToList();

                return MapPedido(pedido, detallesPedido);
            }).ToList();
        }

        private async Task<PedidoDTO> BuildPedidoDtoAsync(Pedido pedido, CancellationToken cancellationToken)
        {
            var detalles = await _db.DetallesPedido
                .AsNoTracking()
                .Where(d => d.IdPedido == pedido.IdPedido)
                .ToListAsync(cancellationToken);

            var platos = await ResolvePlatoNamesAsync(detalles, cancellationToken);
            var detallesDto = detalles
                .Select(detalle => MapDetalle(detalle, platos.GetValueOrDefault(detalle.IdPlato, "Plato desconocido")))
                .ToList();

            return MapPedido(pedido, detallesDto);
        }

        private async Task<Dictionary<Guid, string>> ResolvePlatoNamesAsync(List<DetallePedido> detalles, CancellationToken cancellationToken)
        {
            var platoIds = detalles.Select(d => d.IdPlato).Distinct().ToList();
            if (platoIds.Count == 0)
            {
                return new Dictionary<Guid, string>();
            }

            return await _db.Platos
                .AsNoTracking()
                .Where(p => platoIds.Contains(p.IdPlato))
                .ToDictionaryAsync(p => p.IdPlato, p => p.Nombre, cancellationToken);
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

        private static PedidoDTO MapPedido(Pedido pedido, List<DetallePedidoDTO> detallesDto)
        {
            var total = detallesDto
                .Where(d => d.SeTieneEnCuentaEnFactura)
                .Sum(d => d.Subtotal);

            return new PedidoDTO
            {
                IdPedido = pedido.IdPedido,
                IdMesa = pedido.IdMesa,
                IdFactura = pedido.IdFactura,
                FechaPedido = pedido.FechaPedido,
                FechaModificacion = pedido.FechaModificacion,
                Estado = pedido.Estado,
                Total = total,
                EstaFacturado = pedido.IdFactura.HasValue,
                TieneLineasActivas = detallesDto.Any(d => d.SeTieneEnCuentaEnFactura),
                Detalles = detallesDto
            };
        }

        private static DetallePedidoDTO MapDetalle(DetallePedido detalle, string platoNombre)
        {
            var subtotal = detalle.Estado == EstadoDetallePedido.ACTIVA
                ? detalle.Cantidad * detalle.PrecioUnitario
                : 0;

            return new DetallePedidoDTO
            {
                IdDetallePedido = detalle.IdDetallePedido,
                IdPedido = detalle.IdPedido,
                IdPlato = detalle.IdPlato,
                PlatoNombre = platoNombre,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario,
                Subtotal = subtotal,
                Estado = detalle.Estado,
                FechaCancelacion = detalle.FechaCancelacion,
                SeTieneEnCuentaEnFactura = detalle.Estado == EstadoDetallePedido.ACTIVA
            };
        }

        private static void ValidateEstadoTransition(EstadoPedido current, EstadoPedido next)
        {
            if (current == EstadoPedido.CANCELADO || current == EstadoPedido.ENTREGADO)
            {
                throw new InvalidOperationException("El pedido ya está cerrado y no admite nuevos cambios de estado.");
            }

            var allowedTransitions = new Dictionary<EstadoPedido, EstadoPedido[]>
            {
                [EstadoPedido.PENDIENTE] = new[] { EstadoPedido.CONFIRMADO, EstadoPedido.CANCELADO },
                [EstadoPedido.CONFIRMADO] = new[] { EstadoPedido.PREPARACION, EstadoPedido.CANCELADO },
                [EstadoPedido.PREPARACION] = new[] { EstadoPedido.LISTO, EstadoPedido.CANCELADO },
                [EstadoPedido.LISTO] = new[] { EstadoPedido.ENTREGADO, EstadoPedido.CANCELADO }
            };

            if (!allowedTransitions.TryGetValue(current, out var nextStates) || !nextStates.Contains(next))
            {
                throw new InvalidOperationException($"Transición de estado no válida: {current} -> {next}.");
            }
        }

        private async Task UpdateMesaAvailabilityAsync(Guid? mesaId, CancellationToken cancellationToken)
        {
            if (!mesaId.HasValue)
            {
                return;
            }

            var mesa = await _db.Mesas.FirstOrDefaultAsync(m => m.IdMesa == mesaId.Value, cancellationToken);
            if (mesa == null)
            {
                return;
            }

            var pedidosMesa = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdMesa == mesaId.Value && !p.IdFactura.HasValue && p.Estado != EstadoPedido.CANCELADO)
                .Select(p => p.IdPedido)
                .ToListAsync(cancellationToken);

            var tieneLineasActivas = pedidosMesa.Count > 0 && await _db.DetallesPedido
                .AsNoTracking()
                .AnyAsync(d => pedidosMesa.Contains(d.IdPedido) && d.Estado == EstadoDetallePedido.ACTIVA, cancellationToken);

            mesa.Estado = !tieneLineasActivas;
            await _db.SaveChangesAsync(cancellationToken);
        }

        private static void SetFechaModificacion(Pedido pedido)
        {
            var property = typeof(Pedido).GetProperty(nameof(Pedido.FechaModificacion));
            property?.SetValue(pedido, DateTime.UtcNow);
        }
    }
}
