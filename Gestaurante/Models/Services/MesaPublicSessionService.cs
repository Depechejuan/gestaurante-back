using System.Security.Cryptography;
using System.Text;
using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class MesaPublicSessionService
    {
        private const int SessionDurationHours = 4;

        private readonly AppDbContext _db;
        private readonly PedidoService _pedidoService;

        public MesaPublicSessionService(AppDbContext db, PedidoService pedidoService)
        {
            _db = db;
            _pedidoService = pedidoService;
        }

        public async Task<MesaPublicSessionDTO> OpenOrResumeAsync(Guid mesaId, string? currentSessionToken, CancellationToken cancellationToken = default)
        {
            await CleanupExpiredSessionsAsync(mesaId, cancellationToken);

            var mesa = await _db.Mesas.FirstOrDefaultAsync(m => m.IdMesa == mesaId, cancellationToken);
            if (mesa == null)
            {
                throw new KeyNotFoundException("Mesa no encontrada.");
            }

            var activeSession = await _db.MesaPublicSessions
                .Where(s => s.IdMesa == mesaId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (activeSession != null)
            {
                if (MatchesSessionToken(activeSession, currentSessionToken))
                {
                    activeSession.LastSeenAt = DateTime.UtcNow;
                    activeSession.ExpiresAt = BuildExpiry();
                    await _db.SaveChangesAsync(cancellationToken);

                    return new MesaPublicSessionDTO
                    {
                        IdMesa = mesaId,
                        SessionToken = currentSessionToken!,
                        ExpiresAt = activeSession.ExpiresAt,
                        CanOrder = true,
                        Message = "Sesión recuperada correctamente."
                    };
                }

                throw new InvalidOperationException("La mesa ya está ocupada por otro cliente.");
            }

            if (!mesa.Estado)
            {
                throw new InvalidOperationException("La mesa ya está ocupada y no puede aceptar una nueva sesión pública.");
            }

            var sessionToken = CreateSessionToken();
            var session = new MesaPublicSession(Guid.NewGuid(), mesaId, HashToken(sessionToken), BuildExpiry())
            {
                LastSeenAt = DateTime.UtcNow
            };

            mesa.Estado = false;
            await _db.MesaPublicSessions.AddAsync(session, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return new MesaPublicSessionDTO
            {
                IdMesa = mesaId,
                SessionToken = sessionToken,
                ExpiresAt = session.ExpiresAt,
                CanOrder = true,
                Message = "Sesión creada correctamente."
            };
        }

        public async Task<List<PedidoDTO>> GetPedidosAsync(Guid mesaId, string sessionToken, CancellationToken cancellationToken = default)
        {
            var session = await ValidateSessionAsync(mesaId, sessionToken, cancellationToken);
            return await _pedidoService.GetByMesaPublicSessionAsync(mesaId, session.IdMesaPublicSession, cancellationToken);
        }

        public async Task<PedidoDTO> CreatePedidoAsync(Guid mesaId, string sessionToken, CrearPedidoPublicoDTO dto, CancellationToken cancellationToken = default)
        {
            var session = await ValidateSessionAsync(mesaId, sessionToken, cancellationToken);

            var pedido = await _pedidoService.CreateAsync(new CrearPedidoDTO
            {
                IdMesa = mesaId,
                Estado = EstadoPedido.PENDIENTE,
                CanalPedido = CanalPedido.QR,
                TipoEntrega = TipoEntrega.MESA,
                EstadoPago = EstadoPago.NO_APLICA,
                Detalles = dto.Detalles
            }, session.IdMesaPublicSession, cancellationToken);

            session.LastSeenAt = DateTime.UtcNow;
            session.ExpiresAt = BuildExpiry();
            await _db.SaveChangesAsync(cancellationToken);

            return pedido;
        }

        public async Task InvalidateMesaSessionsAsync(Guid mesaId, CancellationToken cancellationToken = default)
        {
            var activeSessions = await _db.MesaPublicSessions
                .Where(s => s.IdMesa == mesaId && s.IsActive)
                .ToListAsync(cancellationToken);

            if (activeSessions.Count == 0)
            {
                return;
            }

            foreach (var session in activeSessions)
            {
                session.IsActive = false;
                session.ClosedAt = DateTime.UtcNow;
                if (session.ExpiresAt > DateTime.UtcNow)
                {
                    session.ExpiresAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<MesaPublicSession> ValidateSessionAsync(Guid mesaId, string sessionToken, CancellationToken cancellationToken)
        {
            await CleanupExpiredSessionsAsync(mesaId, cancellationToken);

            var tokenHash = HashToken(sessionToken);
            var session = await _db.MesaPublicSessions
                .FirstOrDefaultAsync(
                    s => s.IdMesa == mesaId
                        && s.TokenHash == tokenHash
                        && s.IsActive
                        && s.ExpiresAt > DateTime.UtcNow,
                    cancellationToken);

            if (session == null)
            {
                throw new UnauthorizedAccessException("La sesión pública no es válida o ha expirado.");
            }

            session.LastSeenAt = DateTime.UtcNow;
            session.ExpiresAt = BuildExpiry();
            await _db.SaveChangesAsync(cancellationToken);
            return session;
        }

        private async Task CleanupExpiredSessionsAsync(Guid mesaId, CancellationToken cancellationToken)
        {
            var expiredSessions = await _db.MesaPublicSessions
                .Where(s => s.IdMesa == mesaId && s.IsActive && s.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            if (expiredSessions.Count == 0)
            {
                return;
            }

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
                session.ClosedAt = session.ClosedAt ?? DateTime.UtcNow;
            }

            var mesa = await _db.Mesas.FirstOrDefaultAsync(m => m.IdMesa == mesaId, cancellationToken);
            if (mesa != null)
            {
                var pendingPedidoIds = await _db.Pedidos
                    .AsNoTracking()
                    .Where(p => p.IdMesa == mesaId && !p.IdFactura.HasValue && p.Estado != EstadoPedido.CANCELADO)
                    .Select(p => p.IdPedido)
                    .ToListAsync(cancellationToken);

                var hasActiveLines = pendingPedidoIds.Count > 0 && await _db.DetallesPedido
                    .AsNoTracking()
                    .AnyAsync(d => pendingPedidoIds.Contains(d.IdPedido) && d.Estado == EstadoDetallePedido.ACTIVA, cancellationToken);

                if (!hasActiveLines)
                {
                    mesa.Estado = true;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        private static bool MatchesSessionToken(MesaPublicSession session, string? currentSessionToken)
        {
            return !string.IsNullOrWhiteSpace(currentSessionToken) && session.TokenHash == HashToken(currentSessionToken);
        }

        private static DateTime BuildExpiry()
        {
            return DateTime.UtcNow.AddHours(SessionDurationHours);
        }

        private static string CreateSessionToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}
