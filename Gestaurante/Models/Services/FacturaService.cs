using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class FacturaService
    {
        private readonly AppDbContext _db;
        private readonly MesaPublicSessionService _mesaPublicSessionService;

        public FacturaService(AppDbContext db, MesaPublicSessionService mesaPublicSessionService)
        {
            _db = db;
            _mesaPublicSessionService = mesaPublicSessionService;
        }

        public async Task<List<FacturaDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var facturas = await _db.Facturas
                .AsNoTracking()
                .OrderByDescending(f => f.FechaFactura)
                .ToListAsync(cancellationToken);

            return await BuildFacturaListAsync(facturas, cancellationToken);
        }

        public async Task<FacturaDTO?> GetByIdAsync(Guid numeroFactura, CancellationToken cancellationToken = default)
        {
            var factura = await _db.Facturas
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.NumeroFactura == numeroFactura, cancellationToken);

            if (factura == null)
            {
                return null;
            }

            return await BuildFacturaDtoAsync(factura, cancellationToken);
        }

        public async Task<FacturaDTO> CreateAsync(CrearFacturaDTO dto, CancellationToken cancellationToken = default)
        {
            if (dto.IdMesa.HasValue)
            {
                return await CreateFromMesaAsync(dto.IdMesa.Value, dto.Descuento, dto.Estado, dto.FechaFactura, cancellationToken);
            }

            if (dto.IdPedido.HasValue)
            {
                return await CreateFromPedidoAsync(dto.IdPedido.Value, dto.Descuento, dto.Estado, dto.FechaFactura, cancellationToken);
            }

            if (!dto.PrecioTotal.HasValue)
            {
                throw new InvalidOperationException("Debes indicar una mesa, un pedido o un precio total para la factura.");
            }

            var factura = new Factura(
                Guid.NewGuid(),
                null,
                null,
                dto.PrecioTotal.Value,
                dto.Descuento,
                dto.Estado,
                dto.FechaFactura,
                dto.CanalPedido
            );

            await _db.Facturas.AddAsync(factura, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return await BuildFacturaDtoAsync(factura, cancellationToken);
        }

        public async Task<FacturaDTO> CloseMesaAsync(Guid mesaId, CerrarMesaDTO dto, CancellationToken cancellationToken = default)
        {
            return await CreateFromMesaAsync(mesaId, dto.Descuento, dto.EstadoFactura, dto.FechaFactura, cancellationToken);
        }

        public async Task<FacturaDTO?> UpdateAsync(Guid numeroFactura, EditarFacturaDTO dto, CancellationToken cancellationToken = default)
        {
            var factura = await _db.Facturas.FirstOrDefaultAsync(f => f.NumeroFactura == numeroFactura, cancellationToken);
            if (factura == null)
            {
                return null;
            }

            var linkedPedidoIds = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdFactura == numeroFactura)
                .Select(p => p.IdPedido)
                .ToListAsync(cancellationToken);

            if ((dto.IdMesa.HasValue && dto.IdMesa != factura.IdMesa) || (dto.IdPedido.HasValue && dto.IdPedido != factura.IdPedido))
            {
                throw new InvalidOperationException("No se puede reasignar una factura ya creada a otra mesa o pedido.");
            }

            if (dto.PrecioTotal.HasValue && linkedPedidoIds.Count > 0)
            {
                throw new InvalidOperationException("No se puede sobrescribir manualmente el importe de una factura vinculada a pedidos.");
            }

            if (dto.PrecioTotal.HasValue)
            {
                factura.PrecioTotal = dto.PrecioTotal.Value;
            }

            if (dto.Descuento.HasValue)
            {
                factura.Descuento = dto.Descuento.Value;
            }

            if (dto.Estado.HasValue)
            {
                factura.Estado = dto.Estado.Value;
            }

            if (dto.FechaFactura.HasValue)
            {
                factura.FechaFactura = dto.FechaFactura.Value;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return await BuildFacturaDtoAsync(factura, cancellationToken);
        }

        public async Task<bool> DeleteAsync(Guid numeroFactura, CancellationToken cancellationToken = default)
        {
            var factura = await _db.Facturas.FirstOrDefaultAsync(f => f.NumeroFactura == numeroFactura, cancellationToken);
            if (factura == null)
            {
                return false;
            }

            var hasPedidos = await _db.Pedidos.AnyAsync(p => p.IdFactura == numeroFactura, cancellationToken);
            if (hasPedidos)
            {
                throw new InvalidOperationException("No se puede eliminar una factura con pedidos asociados.");
            }

            _db.Facturas.Remove(factura);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<FacturaDTO> CreateFromPedidoAsync(Guid pedidoId, double descuento, EstadoFactura estado, DateTime? fechaFactura, CancellationToken cancellationToken)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);
            if (pedido == null)
            {
                throw new KeyNotFoundException("Pedido no encontrado.");
            }

            if (pedido.IdFactura.HasValue)
            {
                throw new InvalidOperationException("El pedido ya está facturado.");
            }

            if (pedido.Estado == EstadoPedido.CANCELADO)
            {
                throw new InvalidOperationException("No se puede facturar un pedido cancelado.");
            }

            var total = await ResolvePedidoTotalAsync(pedidoId, cancellationToken);
            if (total <= 0)
            {
                throw new InvalidOperationException("El pedido no tiene líneas activas para facturar.");
            }

            var factura = new Factura(
                Guid.NewGuid(),
                pedido.IdMesa,
                pedidoId,
                total,
                descuento,
                estado,
                fechaFactura,
                pedido.CanalPedido
            );

            await _db.Facturas.AddAsync(factura, cancellationToken);
            pedido.IdFactura = factura.NumeroFactura;

            await _db.SaveChangesAsync(cancellationToken);
            await UpdateMesaAvailabilityAsync(pedido.IdMesa, cancellationToken);
            return await BuildFacturaDtoAsync(factura, cancellationToken);
        }

        private async Task<FacturaDTO> CreateFromMesaAsync(Guid mesaId, double descuento, EstadoFactura estado, DateTime? fechaFactura, CancellationToken cancellationToken)
        {
            var mesa = await _db.Mesas.FirstOrDefaultAsync(m => m.IdMesa == mesaId, cancellationToken);
            if (mesa == null)
            {
                throw new KeyNotFoundException("Mesa no encontrada.");
            }

            var pedidos = await _db.Pedidos
                .Where(p => p.IdMesa == mesaId && !p.IdFactura.HasValue && p.Estado != EstadoPedido.CANCELADO)
                .OrderBy(p => p.FechaPedido)
                .ToListAsync(cancellationToken);

            if (pedidos.Count == 0)
            {
                throw new InvalidOperationException("La mesa no tiene pedidos pendientes de facturar.");
            }

            var pedidoIds = pedidos.Select(p => p.IdPedido).ToList();
            var detalles = await _db.DetallesPedido
                .Where(d => pedidoIds.Contains(d.IdPedido) && d.Estado == EstadoDetallePedido.ACTIVA)
                .ToListAsync(cancellationToken);

            var pedidosFacturables = pedidos
                .Where(p => detalles.Any(d => d.IdPedido == p.IdPedido))
                .ToList();

            if (pedidosFacturables.Count == 0)
            {
                throw new InvalidOperationException("La mesa no tiene líneas activas pendientes de facturar.");
            }

            var total = detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
            var factura = new Factura(
                Guid.NewGuid(),
                mesaId,
                pedidosFacturables.Count == 1 ? pedidosFacturables[0].IdPedido : null,
                total,
                descuento,
                estado,
                fechaFactura,
                pedidosFacturables.FirstOrDefault()?.CanalPedido
            );

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            await _db.Facturas.AddAsync(factura, cancellationToken);

            foreach (var pedido in pedidosFacturables)
            {
                pedido.IdFactura = factura.NumeroFactura;
            }

            mesa.Estado = true;

            await _db.SaveChangesAsync(cancellationToken);
            await _mesaPublicSessionService.InvalidateMesaSessionsAsync(mesaId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await BuildFacturaDtoAsync(factura, cancellationToken);
        }

        private async Task<double> ResolvePedidoTotalAsync(Guid pedidoId, CancellationToken cancellationToken)
        {
            return await _db.DetallesPedido
                .AsNoTracking()
                .Where(d => d.IdPedido == pedidoId && d.Estado == EstadoDetallePedido.ACTIVA)
                .SumAsync(d => d.Cantidad * d.PrecioUnitario, cancellationToken);
        }

        private async Task<List<FacturaDTO>> BuildFacturaListAsync(List<Factura> facturas, CancellationToken cancellationToken)
        {
            var facturaIds = facturas.Select(f => f.NumeroFactura).ToList();
            var pedidosPorFactura = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdFactura.HasValue && facturaIds.Contains(p.IdFactura.Value))
                .GroupBy(p => p.IdFactura!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.Select(p => p.IdPedido).ToList(), cancellationToken);

            return facturas.Select(factura => MapFactura(factura, pedidosPorFactura.GetValueOrDefault(factura.NumeroFactura, new List<Guid>()))).ToList();
        }

        private async Task<FacturaDTO> BuildFacturaDtoAsync(Factura factura, CancellationToken cancellationToken)
        {
            var pedidoIds = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdFactura == factura.NumeroFactura)
                .Select(p => p.IdPedido)
                .ToListAsync(cancellationToken);

            return MapFactura(factura, pedidoIds);
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

            var pedidoIds = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdMesa == mesaId.Value && !p.IdFactura.HasValue && p.Estado != EstadoPedido.CANCELADO)
                .Select(p => p.IdPedido)
                .ToListAsync(cancellationToken);

            var hasActiveLines = pedidoIds.Count > 0 && await _db.DetallesPedido
                .AsNoTracking()
                .AnyAsync(d => pedidoIds.Contains(d.IdPedido) && d.Estado == EstadoDetallePedido.ACTIVA, cancellationToken);

            mesa.Estado = !hasActiveLines;
            await _db.SaveChangesAsync(cancellationToken);
        }

        private static FacturaDTO MapFactura(Factura factura, List<Guid> pedidoIds)
        {
            return new FacturaDTO
            {
                NumeroFactura = factura.NumeroFactura,
                IdMesa = factura.IdMesa,
                IdPedido = factura.IdPedido,
                CanalPedido = factura.CanalPedido,
                PrecioTotal = factura.PrecioTotal,
                Descuento = factura.Descuento,
                TotalConDescuento = Math.Max(0, factura.PrecioTotal - factura.Descuento),
                Estado = factura.Estado,
                FechaFactura = factura.FechaFactura,
                PedidoIds = pedidoIds
            };
        }
    }
}
