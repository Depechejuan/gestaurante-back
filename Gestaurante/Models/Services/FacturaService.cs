using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class FacturaService
    {
        private readonly AppDbContext _db;

        public FacturaService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<FacturaDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var facturas = await _db.Facturas
                .AsNoTracking()
                .OrderByDescending(f => f.FechaFactura)
                .ToListAsync(cancellationToken);

            return facturas.Select(MapFactura).ToList();
        }

        public async Task<FacturaDTO?> GetByIdAsync(Guid numeroFactura, CancellationToken cancellationToken = default)
        {
            var factura = await _db.Facturas
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.NumeroFactura == numeroFactura, cancellationToken);

            return factura == null ? null : MapFactura(factura);
        }

        public async Task<FacturaDTO> CreateAsync(CrearFacturaDTO dto, CancellationToken cancellationToken = default)
        {
            var precioTotal = await ResolvePrecioTotalAsync(dto.IdPedido, dto.PrecioTotal, cancellationToken);

            var factura = new Factura(
                Guid.NewGuid(),
                dto.IdPedido,
                precioTotal,
                dto.Descuento,
                dto.Estado,
                dto.FechaFactura
            );

            await _db.Facturas.AddAsync(factura, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return MapFactura(factura);
        }

        public async Task<FacturaDTO?> UpdateAsync(Guid numeroFactura, EditarFacturaDTO dto, CancellationToken cancellationToken = default)
        {
            var factura = await _db.Facturas.FirstOrDefaultAsync(f => f.NumeroFactura == numeroFactura, cancellationToken);
            if (factura == null) return null;

            if (dto.IdPedido.HasValue && dto.IdPedido.Value != factura.IdPedido)
            {
                factura.IdPedido = dto.IdPedido.Value;
                factura.PrecioTotal = await ResolvePrecioTotalAsync(dto.IdPedido, null, cancellationToken);
            }
            else if (dto.PrecioTotal.HasValue)
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
            return MapFactura(factura);
        }

        public async Task<bool> DeleteAsync(Guid numeroFactura, CancellationToken cancellationToken = default)
        {
            var factura = await _db.Facturas.FirstOrDefaultAsync(f => f.NumeroFactura == numeroFactura, cancellationToken);
            if (factura == null) return false;

            _db.Facturas.Remove(factura);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<double> ResolvePrecioTotalAsync(Guid? idPedido, double? precioTotal, CancellationToken cancellationToken)
        {
            if (idPedido.HasValue)
            {
                var pedidoExists = await _db.Pedidos.AnyAsync(p => p.IdPedido == idPedido.Value, cancellationToken);
                if (!pedidoExists)
                    throw new KeyNotFoundException("Pedido no encontrado.");

                return await _db.DetallesPedido
                    .AsNoTracking()
                    .Where(d => d.IdPedido == idPedido.Value)
                    .SumAsync(d => d.Cantidad * d.PrecioUnitario, cancellationToken);
            }

            if (!precioTotal.HasValue)
                throw new InvalidOperationException("Debes indicar un pedido o un precio total para la factura.");

            return precioTotal.Value;
        }

        private static FacturaDTO MapFactura(Factura factura)
        {
            return new FacturaDTO
            {
                NumeroFactura = factura.NumeroFactura,
                IdPedido = factura.IdPedido,
                PrecioTotal = factura.PrecioTotal,
                Descuento = factura.Descuento,
                TotalConDescuento = Math.Max(0, factura.PrecioTotal - factura.Descuento),
                Estado = factura.Estado,
                FechaFactura = factura.FechaFactura
            };
        }
    }
}
