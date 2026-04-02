using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    /// <summary>
    /// Gestiona el ciclo operativo completo de los pedidos y sus líneas.
    /// </summary>
    public class PedidoService
    {
        private const string PedidoBloqueadoMessage = "El pedido ya fue enviado y no admite cambios estructurales.";

        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;

        /// <summary>
        /// Inicializa el servicio de pedidos.
        /// </summary>
        /// <param name="db">Contexto EF del dominio.</param>
        /// <param name="emailService">Servicio de envío de notificaciones al cliente.</param>
        public PedidoService(AppDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        /// <summary>
        /// Recupera todos los pedidos ordenados por fecha descendente.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Listado completo de pedidos.</returns>
        public async Task<List<PedidoDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var pedidos = await _db.Pedidos
                .AsNoTracking()
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync(cancellationToken);

            return await BuildPedidoListAsync(pedidos, cancellationToken);
        }

        /// <summary>
        /// Recupera el histórico de pedidos de un cliente concreto.
        /// </summary>
        /// <param name="clienteId">Identificador del cliente.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Pedidos asociados al cliente.</returns>
        public async Task<List<PedidoDTO>> GetByClienteAsync(Guid clienteId, CancellationToken cancellationToken = default)
        {
            var pedidos = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdUsuarioCliente == clienteId)
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync(cancellationToken);

            return await BuildPedidoListAsync(pedidos, cancellationToken);
        }

        /// <summary>
        /// Recupera un pedido por su identificador.
        /// </summary>
        /// <param name="pedidoId">Identificador del pedido.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Pedido solicitado o <see langword="null"/> si no existe.</returns>
        public async Task<PedidoDTO?> GetByIdAsync(Guid pedidoId, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);

            return pedido == null ? null : await BuildPedidoDtoAsync(pedido, cancellationToken);
        }

        /// <summary>
        /// Recupera un pedido solo si pertenece al cliente indicado.
        /// </summary>
        /// <param name="clienteId">Identificador del cliente.</param>
        /// <param name="pedidoId">Identificador del pedido.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Pedido solicitado o <see langword="null"/> si no pertenece al cliente.</returns>
        public async Task<PedidoDTO?> GetByClienteAndIdAsync(Guid clienteId, Guid pedidoId, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPedido == pedidoId && p.IdUsuarioCliente == clienteId, cancellationToken);

            return pedido == null ? null : await BuildPedidoDtoAsync(pedido, cancellationToken);
        }

        /// <summary>
        /// Crea un pedido estándar sin sesión pública de mesa asociada.
        /// </summary>
        /// <param name="dto">Datos completos del pedido a crear.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Pedido creado.</returns>
        public async Task<PedidoDTO> CreateAsync(CrearPedidoDTO dto, CancellationToken cancellationToken = default)
        {
            return await CreateAsync(dto, null, cancellationToken);
        }

        /// <summary>
        /// Crea un pedido con sus líneas y, si aplica, lo vincula a una sesión pública de mesa.
        /// </summary>
        /// <param name="dto">Datos completos del pedido a crear.</param>
        /// <param name="mesaPublicSessionId">Sesión pública de mesa asociada, si existe.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Pedido creado y reconstruido como DTO.</returns>
        /// <remarks>
        /// Este método copia los precios de plato en las líneas para preservar el histórico económico.
        /// </remarks>
        public async Task<PedidoDTO> CreateAsync(CrearPedidoDTO dto, Guid? mesaPublicSessionId, CancellationToken cancellationToken = default)
        {
            if (dto.Detalles.Count == 0)
                throw new InvalidOperationException("El pedido debe contener al menos una línea.");

            Mesa? mesa = null;
            if (dto.IdMesa.HasValue)
            {
                mesa = await _db.Mesas.FirstOrDefaultAsync(m => m.IdMesa == dto.IdMesa.Value, cancellationToken);
                if (mesa == null)
                    throw new KeyNotFoundException("Mesa no encontrada.");
            }
            else if (dto.TipoEntrega == TipoEntrega.MESA)
                throw new InvalidOperationException("Los pedidos de sala requieren una mesa.");

            var pedido = new Pedido(
                Guid.NewGuid(),
                dto.IdMesa,
                DateTime.UtcNow,
                dto.Estado,
                mesaPublicSessionId,
                dto.IdUsuarioCliente,
                dto.CanalPedido,
                dto.TipoEntrega,
                dto.EstadoPago)
            {
                ClienteNombre = dto.ClienteNombre?.Trim() ?? string.Empty,
                ClienteEmail = dto.ClienteEmail?.Trim() ?? string.Empty,
                ClienteTelefono = dto.ClienteTelefono?.Trim() ?? string.Empty,
                ClienteDireccionSnapshot = dto.ClienteDireccionSnapshot?.Trim() ?? string.Empty,
                GastosEnvio = dto.GastosEnvio,
                Notas = dto.Notas?.Trim() ?? string.Empty
            };

            await _db.Pedidos.AddAsync(pedido, cancellationToken);

            foreach (var detalleDto in dto.Detalles)
            {
                var detalle = await CreateDetalleInternalAsync(pedido.IdPedido, detalleDto, cancellationToken);
                pedido.DetallesPedido.Add(detalle);
            }

            if (mesa != null)
                mesa.Estado = false;

            await _db.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(pedido.IdPedido, cancellationToken)
                ?? throw new InvalidOperationException("No se pudo recuperar el pedido recién creado.");
        }

        /// <summary>
        /// Recupera los pedidos creados dentro de una sesión pública de mesa concreta.
        /// </summary>
        /// <param name="mesaId">Identificador de la mesa.</param>
        /// <param name="mesaPublicSessionId">Identificador de la sesión pública vinculada al dispositivo.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Pedidos de esa sesión pública de mesa.</returns>
        public async Task<List<PedidoDTO>> GetByMesaPublicSessionAsync(Guid mesaId, Guid mesaPublicSessionId, CancellationToken cancellationToken = default)
        {
            var pedidos = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdMesa == mesaId && p.IdMesaPublicSession == mesaPublicSessionId)
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync(cancellationToken);

            return await BuildPedidoListAsync(pedidos, cancellationToken);
        }

        /// <summary>
        /// Actualiza el estado operativo de un pedido y desencadena sus efectos colaterales.
        /// </summary>
        /// <param name="pedidoId">Identificador del pedido.</param>
        /// <param name="dto">Cambios de estado solicitados.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Pedido actualizado o <see langword="null"/> si no existe.</returns>
        public async Task<PedidoDTO?> UpdateAsync(Guid pedidoId, EditarPedidoDTO dto, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);
            if (pedido == null)
                return null;

            if (pedido.IdFactura.HasValue && dto.Estado == EstadoPedido.CANCELADO)
                throw new InvalidOperationException("No se puede cancelar un pedido ya facturado.");

            if (!dto.Estado.HasValue || dto.Estado.Value == pedido.Estado)
                return await GetByIdAsync(pedidoId, cancellationToken);

            ValidateEstadoTransition(pedido.Estado, dto.Estado.Value);

            pedido.Estado = dto.Estado.Value;
            SetFechaModificacion(pedido);

            if (pedido.CanalPedido == CanalPedido.ONLINE
                && pedido.TipoEntrega == TipoEntrega.RECOGIDA
                && dto.Estado.Value == EstadoPedido.ENTREGADO
                && pedido.EstadoPago == EstadoPago.PENDIENTE_LOCAL
                && !pedido.IdFactura.HasValue)
                await CreateLocalPickupFacturaAsync(pedido, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            await SendPedidoStatusEmailsAsync(pedido, cancellationToken);
            await UpdateMesaAvailabilityAsync(pedido.IdMesa, cancellationToken);

            return await GetByIdAsync(pedidoId, cancellationToken);
        }

        /// <summary>
        /// Elimina físicamente un pedido solo cuando no está facturado.
        /// </summary>
        /// <param name="pedidoId">Identificador del pedido.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns><see langword="true"/> si el pedido se elimina; en otro caso, <see langword="false"/>.</returns>
        public async Task<bool> DeleteAsync(Guid pedidoId, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);
            if (pedido == null)
                return false;

            if (pedido.IdFactura.HasValue)
                throw new InvalidOperationException("No se puede borrar un pedido ya facturado.");

            var detalles = await _db.DetallesPedido.Where(d => d.IdPedido == pedidoId).ToListAsync(cancellationToken);
            if (detalles.Count > 0)
                _db.DetallesPedido.RemoveRange(detalles);

            var mesaId = pedido.IdMesa;
            _db.Pedidos.Remove(pedido);
            await _db.SaveChangesAsync(cancellationToken);
            await UpdateMesaAvailabilityAsync(mesaId, cancellationToken);
            return true;
        }

        /// <summary>
        /// Cancela un pedido completo y anula todas sus líneas activas.
        /// </summary>
        /// <param name="pedidoId">Identificador del pedido.</param>
        /// <param name="dto">Motivo de cancelación si existe en el contrato.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Pedido cancelado o <see langword="null"/> si no existe.</returns>
        public async Task<PedidoDTO?> CancelAsync(Guid pedidoId, CancelarPedidoDTO? dto, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos
                .Include(p => p.DetallesPedido)
                .FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);

            if (pedido == null)
                return null;

            if (pedido.IdFactura.HasValue)
                throw new InvalidOperationException("No se puede cancelar un pedido ya facturado.");

            if (pedido.Estado != EstadoPedido.CANCELADO)
            {
                pedido.Estado = EstadoPedido.CANCELADO;
                foreach (var detalle in pedido.DetallesPedido.Where(d => d.Estado != EstadoDetallePedido.CANCELADA))
                {
                    detalle.Estado = EstadoDetallePedido.CANCELADA;
                    detalle.FechaCancelacion = DateTime.UtcNow;
                }

                SyncPedidoEstadoFromDetalles(pedido);
                SetFechaModificacion(pedido);
                await _db.SaveChangesAsync(cancellationToken);
                await UpdateMesaAvailabilityAsync(pedido.IdMesa, cancellationToken);
            }

            return await GetByIdAsync(pedidoId, cancellationToken);
        }

        /// <summary>
        /// Recupera una línea concreta de pedido.
        /// </summary>
        /// <param name="pedidoId">Identificador del pedido.</param>
        /// <param name="detalleId">Identificador de la línea.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Línea solicitada o <see langword="null"/> si no existe.</returns>
        public async Task<DetallePedidoDTO?> GetDetalleAsync(Guid pedidoId, Guid detalleId, CancellationToken cancellationToken = default)
        {
            var detalle = await _db.DetallesPedido
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.IdPedido == pedidoId && d.IdDetallePedido == detalleId, cancellationToken);

            return detalle == null ? null : await BuildDetalleDtoAsync(detalle, cancellationToken);
        }

        /// <summary>
        /// Impide añadir líneas nuevas a pedidos ya cerrados estructuralmente.
        /// </summary>
        /// <param name="pedidoId">Identificador del pedido.</param>
        /// <param name="dto">Datos de la línea a añadir.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Nunca devuelve una línea nueva; lanza una excepción de negocio.</returns>
        public async Task<DetallePedidoDTO> AddDetalleAsync(Guid pedidoId, CrearDetallePedidoDTO dto, CancellationToken cancellationToken = default)
        {
            await EnsurePedidoExistsAsync(pedidoId, cancellationToken);
            throw new InvalidOperationException(PedidoBloqueadoMessage);
        }

        /// <summary>
        /// Actualiza el estado operativo de una línea de pedido existente.
        /// </summary>
        /// <param name="pedidoId">Identificador del pedido.</param>
        /// <param name="detalleId">Identificador de la línea.</param>
        /// <param name="dto">Cambios permitidos sobre la línea.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Línea actualizada o <see langword="null"/> si no existe.</returns>
        public async Task<DetallePedidoDTO?> UpdateDetalleAsync(Guid pedidoId, Guid detalleId, EditarDetallePedidoDTO dto, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos
                .Include(p => p.DetallesPedido)
                .FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);
            if (pedido == null)
                return null;

            var detalle = pedido.DetallesPedido.FirstOrDefault(d => d.IdDetallePedido == detalleId);
            if (detalle == null)
                return null;

            if (dto.IdPlato.HasValue || dto.Cantidad.HasValue)
                throw new InvalidOperationException(PedidoBloqueadoMessage);

            if (!dto.Estado.HasValue || dto.Estado.Value == detalle.Estado)
                return await BuildDetalleDtoAsync(detalle, cancellationToken);

            if (pedido.IdFactura.HasValue && dto.Estado.Value == EstadoDetallePedido.CANCELADA)
                throw new InvalidOperationException("No se puede cancelar una línea de un pedido ya facturado.");

            ValidateDetalleTransition(detalle.Estado, dto.Estado.Value);

            detalle.Estado = dto.Estado.Value;
            detalle.FechaCancelacion = dto.Estado.Value == EstadoDetallePedido.CANCELADA ? DateTime.UtcNow : null;

            SyncPedidoEstadoFromDetalles(pedido);
            SetFechaModificacion(pedido);

            await _db.SaveChangesAsync(cancellationToken);
            await UpdateMesaAvailabilityAsync(pedido.IdMesa, cancellationToken);

            return await BuildDetalleDtoAsync(detalle, cancellationToken);
        }

        /// <summary>
        /// Impide el borrado físico de líneas en pedidos ya consolidados.
        /// </summary>
        /// <param name="pedidoId">Identificador del pedido.</param>
        /// <param name="detalleId">Identificador de la línea.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Nunca devuelve un borrado exitoso si la línea existe; lanza una excepción de negocio.</returns>
        public async Task<bool> DeleteDetalleAsync(Guid pedidoId, Guid detalleId, CancellationToken cancellationToken = default)
        {
            var detalle = await _db.DetallesPedido
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.IdPedido == pedidoId && d.IdDetallePedido == detalleId, cancellationToken);

            if (detalle == null)
                return false;
            

            throw new InvalidOperationException(PedidoBloqueadoMessage);
        }

        /// <summary>
        /// Cancela una línea concreta del pedido y recalcula el estado global del pedido.
        /// </summary>
        /// <param name="pedidoId">Identificador del pedido.</param>
        /// <param name="detalleId">Identificador de la línea a cancelar.</param>
        /// <param name="dto">Motivo de cancelación si existe en el contrato.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Línea cancelada o <see langword="null"/> si no existe.</returns>
        public async Task<DetallePedidoDTO?> CancelDetalleAsync(Guid pedidoId, Guid detalleId, CancelarDetallePedidoDTO? dto, CancellationToken cancellationToken = default)
        {
            var pedido = await _db.Pedidos
                .Include(p => p.DetallesPedido)
                .FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);

            if (pedido == null)
                return null;

            if (pedido.IdFactura.HasValue)
                throw new InvalidOperationException("No se puede cancelar una línea de un pedido ya facturado.");

            var detalle = pedido.DetallesPedido.FirstOrDefault(d => d.IdDetallePedido == detalleId);
            if (detalle == null)
                return null;

            if (detalle.Estado != EstadoDetallePedido.CANCELADA)
            {
                detalle.Estado = EstadoDetallePedido.CANCELADA;
                detalle.FechaCancelacion = DateTime.UtcNow;
                SyncPedidoEstadoFromDetalles(pedido);
                SetFechaModificacion(pedido);

                await _db.SaveChangesAsync(cancellationToken);
                await UpdateMesaAvailabilityAsync(pedido.IdMesa, cancellationToken);
            }

            return await BuildDetalleDtoAsync(detalle, cancellationToken);
        }

        /// <summary>
        /// Comprueba que el pedido exista antes de ejecutar operaciones bloqueadas.
        /// </summary>
        private async Task EnsurePedidoExistsAsync(Guid pedidoId, CancellationToken cancellationToken)
        {
            var exists = await _db.Pedidos.AnyAsync(p => p.IdPedido == pedidoId, cancellationToken);
            if (!exists)
                throw new KeyNotFoundException("Pedido no encontrado.");
        }

        /// <summary>
        /// Crea una línea interna copiando el precio vigente del plato en el momento del pedido.
        /// </summary>
        private async Task<DetallePedido> CreateDetalleInternalAsync(Guid pedidoId, CrearDetallePedidoDTO dto, CancellationToken cancellationToken)
        {
            var plato = await _db.Platos.FirstOrDefaultAsync(p => p.IdPlato == dto.IdPlato, cancellationToken);
            if (plato == null)
                throw new KeyNotFoundException("Plato no encontrado.");

            if (!plato.Disponible)
                throw new InvalidOperationException($"El plato {plato.Nombre} no está disponible.");

            return new DetallePedido(Guid.NewGuid(), plato.IdPlato, pedidoId, dto.Cantidad, Convert.ToDouble(plato.Precio));
        }

        /// <summary>
        /// Construye una lista de pedidos completa resolviendo sus líneas y nombres de plato.
        /// </summary>
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

        /// <summary>
        /// Construye el DTO completo de un pedido individual.
        /// </summary>
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

        /// <summary>
        /// Resuelve los nombres de plato necesarios para pintar las líneas de pedido.
        /// </summary>
        private async Task<Dictionary<Guid, string>> ResolvePlatoNamesAsync(List<DetallePedido> detalles, CancellationToken cancellationToken)
        {
            var platoIds = detalles.Select(d => d.IdPlato).Distinct().ToList();
            if (platoIds.Count == 0)
                return new Dictionary<Guid, string>();

            return await _db.Platos
                .AsNoTracking()
                .Where(p => platoIds.Contains(p.IdPlato))
                .ToDictionaryAsync(p => p.IdPlato, p => p.Nombre, cancellationToken);
        }

        /// <summary>
        /// Construye el DTO de una línea concreta resolviendo el nombre del plato.
        /// </summary>
        private async Task<DetallePedidoDTO> BuildDetalleDtoAsync(DetallePedido detalle, CancellationToken cancellationToken)
        {
            var platoNombre = await _db.Platos
                .AsNoTracking()
                .Where(p => p.IdPlato == detalle.IdPlato)
                .Select(p => p.Nombre)
                .FirstOrDefaultAsync(cancellationToken) ?? "Plato desconocido";

            return MapDetalle(detalle, platoNombre);
        }

        /// <summary>
        /// Mapea un pedido de dominio al DTO consumido por las pantallas operativas.
        /// </summary>
        private static PedidoDTO MapPedido(Pedido pedido, List<DetallePedidoDTO> detallesDto)
        {
            var subtotalProductos = detallesDto
                .Where(d => d.SeTieneEnCuentaEnFactura)
                .Sum(d => d.Subtotal);
            var total = subtotalProductos + pedido.GastosEnvio;

            return new PedidoDTO
            {
                IdPedido = pedido.IdPedido,
                IdMesa = pedido.IdMesa,
                IdFactura = pedido.IdFactura,
                IdUsuarioCliente = pedido.IdUsuarioCliente,
                FechaPedido = pedido.FechaPedido,
                FechaModificacion = pedido.FechaModificacion,
                Estado = pedido.Estado,
                CanalPedido = pedido.CanalPedido,
                TipoEntrega = pedido.TipoEntrega,
                EstadoPago = pedido.EstadoPago,
                ClienteNombre = pedido.ClienteNombre,
                ClienteEmail = pedido.ClienteEmail,
                ClienteTelefono = pedido.ClienteTelefono,
                ClienteDireccionSnapshot = pedido.ClienteDireccionSnapshot,
                Notas = pedido.Notas,
                SubtotalProductos = subtotalProductos,
                GastosEnvio = pedido.GastosEnvio,
                Total = total,
                EstaFacturado = pedido.IdFactura.HasValue,
                TieneLineasActivas = detallesDto.Any(d => d.SeTieneEnCuentaEnFactura),
                Detalles = detallesDto
            };
        }

        /// <summary>
        /// Mapea una línea de pedido al DTO público e interno.
        /// </summary>
        private static DetallePedidoDTO MapDetalle(DetallePedido detalle, string platoNombre)
        {
            var subtotal = detalle.Estado != EstadoDetallePedido.CANCELADA
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
                SeTieneEnCuentaEnFactura = detalle.Estado != EstadoDetallePedido.CANCELADA
            };
        }

        /// <summary>
        /// Valida que la transición de estado del pedido sea coherente con el flujo operativo.
        /// </summary>
        private static void ValidateEstadoTransition(EstadoPedido current, EstadoPedido next)
        {
            if (current == EstadoPedido.CANCELADO || current == EstadoPedido.ENTREGADO)
                throw new InvalidOperationException("El pedido ya está cerrado y no admite nuevos cambios de estado.");

            var allowedTransitions = new Dictionary<EstadoPedido, EstadoPedido[]>
            {
                [EstadoPedido.PENDIENTE] = new[] { EstadoPedido.CONFIRMADO, EstadoPedido.CANCELADO },
                [EstadoPedido.CONFIRMADO] = new[] { EstadoPedido.PREPARACION, EstadoPedido.CANCELADO },
                [EstadoPedido.PREPARACION] = new[] { EstadoPedido.LISTO, EstadoPedido.CANCELADO },
                [EstadoPedido.LISTO] = new[] { EstadoPedido.ENTREGADO, EstadoPedido.EN_CAMINO, EstadoPedido.CANCELADO },
                [EstadoPedido.EN_CAMINO] = new[] { EstadoPedido.ENTREGADO }
            };

            if (!allowedTransitions.TryGetValue(current, out var nextStates) || !nextStates.Contains(next))
                throw new InvalidOperationException($"Transición de estado no válida: {current} -> {next}.");
        }

        /// <summary>
        /// Valida que la transición de estado de una línea sea coherente con cocina y entrega.
        /// </summary>
        private static void ValidateDetalleTransition(EstadoDetallePedido current, EstadoDetallePedido next)
        {
            if (current == EstadoDetallePedido.CANCELADA || current == EstadoDetallePedido.ENTREGADA)
                throw new InvalidOperationException("La línea ya está cerrada y no admite más cambios.");

            var allowedTransitions = new Dictionary<EstadoDetallePedido, EstadoDetallePedido[]>
            {
                [EstadoDetallePedido.ACTIVA] = new[] { EstadoDetallePedido.EN_COCINA, EstadoDetallePedido.ENTREGADA, EstadoDetallePedido.CANCELADA },
                [EstadoDetallePedido.EN_COCINA] = new[] { EstadoDetallePedido.PREPARADO, EstadoDetallePedido.CANCELADA },
                [EstadoDetallePedido.PREPARADO] = new[] { EstadoDetallePedido.ENTREGADA, EstadoDetallePedido.CANCELADA }
            };

            if (!allowedTransitions.TryGetValue(current, out var nextStates) || !nextStates.Contains(next))
                throw new InvalidOperationException($"Transición de línea no válida: {current} -> {next}.");
        }

        /// <summary>
        /// Sincroniza el estado global del pedido a partir del estado de sus líneas.
        /// </summary>
        private static void SyncPedidoEstadoFromDetalles(Pedido pedido)
        {
            var detallesVivos = pedido.DetallesPedido
                .Where(d => d.Estado != EstadoDetallePedido.CANCELADA)
                .ToList();

            if (detallesVivos.Count == 0)
            {
                pedido.Estado = EstadoPedido.CANCELADO;
                return;
            }

            if (detallesVivos.All(d => d.Estado == EstadoDetallePedido.ENTREGADA))
            {
                pedido.Estado = EstadoPedido.ENTREGADO;
                return;
            }

            if (pedido.Estado == EstadoPedido.EN_CAMINO)
                return;

            if (detallesVivos.Any(d => d.Estado == EstadoDetallePedido.PREPARADO))
            {
                pedido.Estado = EstadoPedido.LISTO;
                return;
            }

            if (detallesVivos.Any(d => d.Estado == EstadoDetallePedido.EN_COCINA))
            {
                pedido.Estado = EstadoPedido.PREPARACION;
                return;
            }

            pedido.Estado = detallesVivos.Any(d => d.Estado == EstadoDetallePedido.ENTREGADA)
                ? EstadoPedido.CONFIRMADO
                : EstadoPedido.PENDIENTE;
        }

        /// <summary>
        /// Actualiza el estado de ocupación de la mesa según los pedidos pendientes y sus líneas activas.
        /// </summary>
        private async Task UpdateMesaAvailabilityAsync(Guid? mesaId, CancellationToken cancellationToken)
        {
            if (!mesaId.HasValue)
                return;

            var mesa = await _db.Mesas.FirstOrDefaultAsync(m => m.IdMesa == mesaId.Value, cancellationToken);
            if (mesa == null)
                return;

            var pedidosMesa = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdMesa == mesaId.Value && !p.IdFactura.HasValue && p.Estado != EstadoPedido.CANCELADO)
                .Select(p => p.IdPedido)
                .ToListAsync(cancellationToken);

            var tieneLineasActivas = pedidosMesa.Count > 0 && await _db.DetallesPedido
                .AsNoTracking()
                .AnyAsync(d => pedidosMesa.Contains(d.IdPedido) && d.Estado != EstadoDetallePedido.CANCELADA, cancellationToken);

            mesa.Estado = !tieneLineasActivas;
            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Genera automáticamente la factura de recogida cobrada en local al entregar un pedido online.
        /// </summary>
        private async Task CreateLocalPickupFacturaAsync(Pedido pedido, CancellationToken cancellationToken)
        {
            var total = await _db.DetallesPedido
                .Where(d => d.IdPedido == pedido.IdPedido && d.Estado != EstadoDetallePedido.CANCELADA)
                .SumAsync(d => d.Cantidad * d.PrecioUnitario, cancellationToken);

            if (total <= 0)
                throw new InvalidOperationException("El pedido no tiene líneas activas para facturar.");

            var factura = new Factura(
                Guid.NewGuid(),
                null,
                pedido.IdPedido,
                total,
                0,
                EstadoFactura.PAGADO,
                DateTime.UtcNow,
                pedido.CanalPedido
            );

            await _db.Facturas.AddAsync(factura, cancellationToken);
            pedido.IdFactura = factura.NumeroFactura;
            pedido.EstadoPago = EstadoPago.PAGADO_LOCAL;
        }

        /// <summary>
        /// Envía correos de estado al cliente cuando el pedido online alcanza hitos relevantes.
        /// </summary>
        private async Task SendPedidoStatusEmailsAsync(Pedido pedido, CancellationToken cancellationToken)
        {
            if (pedido.CanalPedido != CanalPedido.ONLINE || string.IsNullOrWhiteSpace(pedido.ClienteEmail))
                return;

            if (pedido.TipoEntrega == TipoEntrega.RECOGIDA && pedido.Estado == EstadoPedido.LISTO)
                await _emailService.SendAsync(
                    pedido.ClienteEmail,
                    "Tu pedido está listo para recoger",
                    $"Hola {pedido.ClienteNombre}, tu pedido {pedido.IdPedido} ya está listo para recoger en el restaurante.",
                    cancellationToken: cancellationToken);

            if (pedido.TipoEntrega == TipoEntrega.DOMICILIO && pedido.Estado == EstadoPedido.EN_CAMINO)
                await _emailService.SendAsync(
                    pedido.ClienteEmail,
                    "Tu pedido ya está en camino",
                    $"Hola {pedido.ClienteNombre}, tu pedido {pedido.IdPedido} ya está en camino.",
                    cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Registra la última fecha de modificación del pedido sin exponer setters públicos adicionales.
        /// </summary>
        private static void SetFechaModificacion(Pedido pedido)
        {
            var property = typeof(Pedido).GetProperty(nameof(Pedido.FechaModificacion));
            property?.SetValue(pedido, DateTime.UtcNow);
        }
    }
}
