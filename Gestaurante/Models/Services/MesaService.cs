using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class MesaService
    {
        private readonly AppDbContext _db;

        public MesaService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<MesaDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var mesas = await _db.Mesas
                .AsNoTracking()
                .OrderBy(m => m.Ubicacion)
                .ThenBy(m => m.Capacidad)
                .ToListAsync(cancellationToken);

            var resumenes = await BuildMesaSummariesAsync(mesas.Select(m => m.IdMesa).ToList(), cancellationToken);

            return mesas.Select(mesa => MapMesa(mesa, resumenes.GetValueOrDefault(mesa.IdMesa))).ToList();
        }

        public async Task<MesaDetalleDTO?> GetByIdAsync(Guid idMesa, CancellationToken cancellationToken = default)
        {
            var mesa = await _db.Mesas
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdMesa == idMesa, cancellationToken);

            if (mesa == null)
            {
                return null;
            }

            var pedidos = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdMesa == idMesa)
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync(cancellationToken);

            var detalles = await _db.DetallesPedido
                .AsNoTracking()
                .Where(d => pedidos.Select(p => p.IdPedido).Contains(d.IdPedido))
                .ToListAsync(cancellationToken);

            var platos = await ResolvePlatoNamesAsync(detalles, cancellationToken);
            var pedidosDto = pedidos.Select(pedido =>
            {
                var detallesDto = detalles
                    .Where(d => d.IdPedido == pedido.IdPedido)
                    .Select(detalle => MapDetalle(detalle, platos.GetValueOrDefault(detalle.IdPlato, "Plato desconocido")))
                    .ToList();

                return MapPedido(pedido, detallesDto);
            }).ToList();

            var resumen = BuildMesaResumen(pedidosDto);
            return new MesaDetalleDTO
            {
                IdMesa = mesa.IdMesa,
                Capacidad = mesa.Capacidad,
                Estado = mesa.Estado,
                Ubicacion = mesa.Ubicacion,
                PedidosAbiertos = resumen.PedidosAbiertos,
                TotalPendienteFactura = resumen.TotalPendienteFactura,
                TienePedidosActivos = resumen.TienePedidosActivos,
                Pedidos = pedidosDto
            };
        }

        public async Task<MesaDTO> CreateAsync(CrearMesaDTO dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Ubicacion))
            {
                throw new InvalidOperationException("La ubicación de la mesa es obligatoria.");
            }

            var mesa = new Mesa(Guid.NewGuid(), dto.Capacidad, dto.Estado, dto.Ubicacion.Trim());
            await _db.Mesas.AddAsync(mesa, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return MapMesa(mesa, null);
        }

        public async Task<MesaDTO?> UpdateAsync(Guid idMesa, EditarMesaDTO dto, CancellationToken cancellationToken = default)
        {
            var mesa = await _db.Mesas.FirstOrDefaultAsync(m => m.IdMesa == idMesa, cancellationToken);
            if (mesa == null)
            {
                return null;
            }

            if (dto.Capacidad.HasValue)
            {
                mesa.Capacidad = dto.Capacidad.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Ubicacion))
            {
                mesa.Ubicacion = dto.Ubicacion.Trim();
            }

            if (dto.Estado.HasValue)
            {
                if (dto.Estado.Value && await TienePedidosActivosAsync(idMesa, cancellationToken))
                {
                    throw new InvalidOperationException("No se puede marcar la mesa como disponible mientras tenga pedidos activos pendientes de facturar.");
                }

                mesa.Estado = dto.Estado.Value;
            }

            await _db.SaveChangesAsync(cancellationToken);
            var resumen = await BuildMesaSummariesAsync(new List<Guid> { idMesa }, cancellationToken);
            return MapMesa(mesa, resumen.GetValueOrDefault(idMesa));
        }

        public async Task<bool> DeleteAsync(Guid idMesa, CancellationToken cancellationToken = default)
        {
            var mesa = await _db.Mesas.FirstOrDefaultAsync(m => m.IdMesa == idMesa, cancellationToken);
            if (mesa == null)
            {
                return false;
            }

            var hasPedidos = await _db.Pedidos.AnyAsync(p => p.IdMesa == idMesa, cancellationToken);
            if (hasPedidos)
            {
                throw new InvalidOperationException("No se puede eliminar una mesa con pedidos asociados.");
            }

            _db.Mesas.Remove(mesa);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<Dictionary<Guid, MesaResumen>> BuildMesaSummariesAsync(List<Guid> mesaIds, CancellationToken cancellationToken)
        {
            var pedidos = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdMesa.HasValue && mesaIds.Contains(p.IdMesa.Value))
                .ToListAsync(cancellationToken);

            if (pedidos.Count == 0)
            {
                return mesaIds.ToDictionary(id => id, _ => new MesaResumen());
            }

            var pedidoIds = pedidos.Select(p => p.IdPedido).ToList();
            var detalles = await _db.DetallesPedido
                .AsNoTracking()
                .Where(d => pedidoIds.Contains(d.IdPedido))
                .ToListAsync(cancellationToken);

            return mesaIds.ToDictionary(
                id => id,
                id =>
                {
                    var pedidosMesa = pedidos.Where(p => p.IdMesa == id).ToList();
                    var detallesMesa = detalles.Where(d => pedidosMesa.Select(p => p.IdPedido).Contains(d.IdPedido)).ToList();
                    return BuildMesaResumen(pedidosMesa, detallesMesa);
                });
        }

        private static MesaResumen BuildMesaResumen(List<PedidoDTO> pedidos)
        {
            return new MesaResumen
            {
                PedidosAbiertos = pedidos.Count(p => !p.EstaFacturado && p.Estado != EstadoPedido.CANCELADO && p.TieneLineasActivas),
                TotalPendienteFactura = pedidos
                    .Where(p => !p.EstaFacturado && p.Estado != EstadoPedido.CANCELADO)
                    .Sum(p => p.Total),
                TienePedidosActivos = pedidos.Any(p => !p.EstaFacturado && p.Estado != EstadoPedido.CANCELADO && p.TieneLineasActivas)
            };
        }

        private static MesaResumen BuildMesaResumen(List<Pedido> pedidos, List<DetallePedido> detalles)
        {
            var pedidosActivos = pedidos
                .Where(p => !p.IdFactura.HasValue && p.Estado != EstadoPedido.CANCELADO)
                .ToList();

            var totalPendiente = 0d;
            var pedidosAbiertos = 0;

            foreach (var pedido in pedidosActivos)
            {
                var totalPedido = detalles
                    .Where(d => d.IdPedido == pedido.IdPedido && d.Estado == EstadoDetallePedido.ACTIVA)
                    .Sum(d => d.Cantidad * d.PrecioUnitario);

                if (totalPedido > 0)
                {
                    pedidosAbiertos++;
                    totalPendiente += totalPedido;
                }
            }

            return new MesaResumen
            {
                PedidosAbiertos = pedidosAbiertos,
                TotalPendienteFactura = totalPendiente,
                TienePedidosActivos = pedidosAbiertos > 0
            };
        }

        private async Task<bool> TienePedidosActivosAsync(Guid idMesa, CancellationToken cancellationToken)
        {
            var pedidoIds = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdMesa == idMesa && !p.IdFactura.HasValue && p.Estado != EstadoPedido.CANCELADO)
                .Select(p => p.IdPedido)
                .ToListAsync(cancellationToken);

            return pedidoIds.Count > 0 && await _db.DetallesPedido
                .AsNoTracking()
                .AnyAsync(d => pedidoIds.Contains(d.IdPedido) && d.Estado == EstadoDetallePedido.ACTIVA, cancellationToken);
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

        private static MesaDTO MapMesa(Mesa mesa, MesaResumen? resumen)
        {
            return new MesaDTO
            {
                IdMesa = mesa.IdMesa,
                Capacidad = mesa.Capacidad,
                Estado = mesa.Estado,
                Ubicacion = mesa.Ubicacion,
                PedidosAbiertos = resumen?.PedidosAbiertos ?? 0,
                TotalPendienteFactura = resumen?.TotalPendienteFactura ?? 0,
                TienePedidosActivos = resumen?.TienePedidosActivos ?? false
            };
        }

        private static PedidoDTO MapPedido(Pedido pedido, List<DetallePedidoDTO> detallesDto)
        {
            return new PedidoDTO
            {
                IdPedido = pedido.IdPedido,
                IdMesa = pedido.IdMesa,
                IdFactura = pedido.IdFactura,
                FechaPedido = pedido.FechaPedido,
                FechaModificacion = pedido.FechaModificacion,
                Estado = pedido.Estado,
                Total = detallesDto.Where(d => d.SeTieneEnCuentaEnFactura).Sum(d => d.Subtotal),
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

        private sealed class MesaResumen
        {
            public int PedidosAbiertos { get; set; }
            public double TotalPendienteFactura { get; set; }
            public bool TienePedidosActivos { get; set; }
        }
    }
}
