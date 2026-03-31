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
        private readonly IEmailService _emailService;

        public FacturaService(AppDbContext db, MesaPublicSessionService mesaPublicSessionService, IEmailService emailService)
        {
            _db = db;
            _mesaPublicSessionService = mesaPublicSessionService;
            _emailService = emailService;
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
                return null;

            return await BuildFacturaDtoAsync(factura, cancellationToken);
        }

        public async Task<FacturaDTO> CreateAsync(CrearFacturaDTO dto, CancellationToken cancellationToken = default)
        {
            if (dto.IdMesa.HasValue)
                return await CreateFromMesaAsync(dto.IdMesa.Value, dto.Descuento, dto.Estado, dto.FechaFactura, cancellationToken);


            if (dto.IdPedido.HasValue)
                return await CreateFromPedidoAsync(dto.IdPedido.Value, dto.Descuento, dto.Estado, dto.FechaFactura, cancellationToken);


            if (!dto.PrecioTotal.HasValue)
                throw new InvalidOperationException("Debes indicar una mesa, un pedido o un precio total para la factura.");


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
            ApplyAnonymousBillingSnapshot(factura);

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
                return null;

            var linkedPedidoIds = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdFactura == numeroFactura)
                .Select(p => p.IdPedido)
                .ToListAsync(cancellationToken);

            if ((dto.IdMesa.HasValue && dto.IdMesa != factura.IdMesa) || (dto.IdPedido.HasValue && dto.IdPedido != factura.IdPedido))
                throw new InvalidOperationException("No se puede reasignar una factura ya creada a otra mesa o pedido.");


            if (dto.PrecioTotal.HasValue && linkedPedidoIds.Count > 0)
                throw new InvalidOperationException("No se puede sobrescribir manualmente el importe de una factura vinculada a pedidos.");


            if (dto.PrecioTotal.HasValue)
                factura.PrecioTotal = dto.PrecioTotal.Value;


            if (dto.Descuento.HasValue)
                factura.Descuento = dto.Descuento.Value;


            if (dto.Estado.HasValue)
                factura.Estado = dto.Estado.Value;

            if (dto.FechaFactura.HasValue)
                factura.FechaFactura = dto.FechaFactura.Value;

            await _db.SaveChangesAsync(cancellationToken);
            return await BuildFacturaDtoAsync(factura, cancellationToken);
        }

        public async Task<List<FacturaClienteLookupDTO>> SearchClientesAsync(string query, CancellationToken cancellationToken = default)
        {
            var term = query.Trim();
            if (string.IsNullOrWhiteSpace(term))
                return new List<FacturaClienteLookupDTO>();

            var lowered = term.ToLower();
            return await _db.UsuariosCliente
                .AsNoTracking()
                .Where(u =>
                    u.Email.ToLower().Contains(lowered)
                    || u.FirstName.ToLower().Contains(lowered)
                    || u.LastName.ToLower().Contains(lowered)
                    || u.FiscalName.ToLower().Contains(lowered)
                    || u.Dni.ToLower().Contains(lowered)
                    || u.Cif.ToLower().Contains(lowered))
                .OrderBy(u => u.FiscalName)
                .ThenBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Take(12)
                .Select(u => new FacturaClienteLookupDTO
                {
                    IdUsuarioCliente = u.IdUsuarioCliente,
                    Email = u.Email,
                    FullName = $"{u.FirstName} {u.LastName}".Trim(),
                    FiscalName = u.FiscalName,
                    Dni = u.Dni,
                    Cif = u.Cif,
                    BillingStreet = u.BillingStreet,
                    BillingCity = u.BillingCity,
                    BillingProvince = u.BillingProvince,
                    BillingPostalCode = u.BillingPostalCode,
                    Phone = u.Phone
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<FacturaDTO?> AssignClienteAsync(Guid numeroFactura, AsignarFacturaClienteDTO dto, CancellationToken cancellationToken = default)
        {
            var factura = await _db.Facturas.FirstOrDefaultAsync(f => f.NumeroFactura == numeroFactura, cancellationToken);
            if (factura == null)
                return null;

            var cliente = await ResolveClienteForFacturaAsync(dto, cancellationToken);
            if (cliente != null)
            {
                if (dto.SaveOnCustomer)
                {
                    UpdateCustomerFiscalData(cliente, dto);
                    cliente.UpdatedAt = DateTime.UtcNow;
                }

                ApplyCustomerBillingSnapshot(factura, cliente, dto);
            }
            else
            {
                ApplyManualBillingSnapshot(factura, dto);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return await BuildFacturaDtoAsync(factura, cancellationToken);
        }

        public async Task<string> SendFacturaEmailAsync(Guid numeroFactura, string? requestedEmail, CancellationToken cancellationToken = default)
        {
            var factura = await GetByIdAsync(numeroFactura, cancellationToken)
                ?? throw new KeyNotFoundException("Factura no encontrada.");

            var email = ResolveFacturaEmailTarget(factura, requestedEmail);
            var subject = $"Factura {factura.NumeroFactura}";
            var body = BuildFacturaEmailBody(factura);

            await _emailService.SendAsync(email, subject, body, cancellationToken);
            return email;
        }

        public async Task<bool> DeleteAsync(Guid numeroFactura, CancellationToken cancellationToken = default)
        {
            var factura = await _db.Facturas.FirstOrDefaultAsync(f => f.NumeroFactura == numeroFactura, cancellationToken);
            if (factura == null)
                return false;

            var hasPedidos = await _db.Pedidos.AnyAsync(p => p.IdFactura == numeroFactura, cancellationToken);
            if (hasPedidos)
                throw new InvalidOperationException("No se puede eliminar una factura con pedidos asociados.");

            _db.Facturas.Remove(factura);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<FacturaDTO> CreateFromPedidoAsync(Guid pedidoId, double descuento, EstadoFactura estado, DateTime? fechaFactura, CancellationToken cancellationToken)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.IdPedido == pedidoId, cancellationToken);
            if (pedido == null)
                throw new KeyNotFoundException("Pedido no encontrado.");

            if (pedido.IdFactura.HasValue)
                throw new InvalidOperationException("El pedido ya está facturado.");

            if (pedido.Estado == EstadoPedido.CANCELADO)
                throw new InvalidOperationException("No se puede facturar un pedido cancelado.");

            var total = await ResolvePedidoTotalAsync(pedidoId, cancellationToken);
            if (total <= 0)
                throw new InvalidOperationException("El pedido no tiene líneas activas para facturar.");

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
            ApplyAnonymousBillingSnapshot(factura);

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
                throw new KeyNotFoundException("Mesa no encontrada.");

            var pedidos = await _db.Pedidos
                .Where(p => p.IdMesa == mesaId && !p.IdFactura.HasValue && p.Estado != EstadoPedido.CANCELADO)
                .OrderBy(p => p.FechaPedido)
                .ToListAsync(cancellationToken);

            if (pedidos.Count == 0)
                throw new InvalidOperationException("La mesa no tiene pedidos pendientes de facturar.");

            var pedidoIds = pedidos.Select(p => p.IdPedido).ToList();
            var detalles = await _db.DetallesPedido
                .Where(d => pedidoIds.Contains(d.IdPedido) && d.Estado == EstadoDetallePedido.ACTIVA)
                .ToListAsync(cancellationToken);

            var pedidosFacturables = pedidos
                .Where(p => detalles.Any(d => d.IdPedido == p.IdPedido))
                .ToList();

            if (pedidosFacturables.Count == 0)
                throw new InvalidOperationException("La mesa no tiene líneas activas pendientes de facturar.");

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
            ApplyAnonymousBillingSnapshot(factura);

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
            var subtotal = await _db.DetallesPedido
                .AsNoTracking()
                .Where(d => d.IdPedido == pedidoId && d.Estado == EstadoDetallePedido.ACTIVA)
                .SumAsync(d => d.Cantidad * d.PrecioUnitario, cancellationToken);

            var gastosEnvio = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdPedido == pedidoId)
                .Select(p => p.GastosEnvio)
                .FirstOrDefaultAsync(cancellationToken);

            return subtotal + gastosEnvio;
        }

        private async Task<List<FacturaDTO>> BuildFacturaListAsync(List<Factura> facturas, CancellationToken cancellationToken)
        {
            var facturaIds = facturas.Select(f => f.NumeroFactura).ToList();
            var pedidosPorFactura = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdFactura.HasValue && facturaIds.Contains(p.IdFactura.Value))
                .GroupBy(p => p.IdFactura!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.Select(p => p.IdPedido).ToList(), cancellationToken);

            return facturas
                .Select(factura => MapFactura(
                    factura,
                    pedidosPorFactura.GetValueOrDefault(factura.NumeroFactura, new List<Guid>()),
                    new List<FacturaLineaDTO>()))
                .ToList();
        }

        private async Task<FacturaDTO> BuildFacturaDtoAsync(Factura factura, CancellationToken cancellationToken)
        {
            var pedidos = await _db.Pedidos
                .AsNoTracking()
                .Where(p => p.IdFactura == factura.NumeroFactura)
                .OrderBy(p => p.FechaPedido)
                .ToListAsync(cancellationToken);

            var pedidoIds = pedidos.Select(p => p.IdPedido).ToList();
            var detalles = await _db.DetallesPedido
                .AsNoTracking()
                .Where(d => pedidoIds.Contains(d.IdPedido) && d.Estado == EstadoDetallePedido.ACTIVA)
                .OrderBy(d => d.IdPedido)
                .ToListAsync(cancellationToken);

            var platoIds = detalles.Select(d => d.IdPlato).Distinct().ToList();
            var platos = platoIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _db.Platos
                    .AsNoTracking()
                    .Where(p => platoIds.Contains(p.IdPlato))
                    .ToDictionaryAsync(p => p.IdPlato, p => p.Nombre, cancellationToken);

            var lineas = detalles.Select(detalle => new FacturaLineaDTO
            {
                IdDetallePedido = detalle.IdDetallePedido,
                IdPedido = detalle.IdPedido,
                IdPlato = detalle.IdPlato,
                PlatoNombre = platos.GetValueOrDefault(detalle.IdPlato, "Plato desconocido"),
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario,
                TotalLinea = detalle.Cantidad * detalle.PrecioUnitario
            }).ToList();

            return MapFactura(factura, pedidoIds, lineas);
        }

        private async Task UpdateMesaAvailabilityAsync(Guid? mesaId, CancellationToken cancellationToken)
        {
            if (!mesaId.HasValue)
                return;

            var mesa = await _db.Mesas.FirstOrDefaultAsync(m => m.IdMesa == mesaId.Value, cancellationToken);
            if (mesa == null)
                return;

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

        private static FacturaDTO MapFactura(Factura factura, List<Guid> pedidoIds, List<FacturaLineaDTO> lineas)
        {
            return new FacturaDTO
            {
                NumeroFactura = factura.NumeroFactura,
                IdMesa = factura.IdMesa,
                IdPedido = factura.IdPedido,
                IdUsuarioCliente = factura.IdUsuarioCliente,
                CanalPedido = factura.CanalPedido,
                PrecioTotal = factura.PrecioTotal,
                Descuento = factura.Descuento,
                TotalConDescuento = Math.Max(0, factura.PrecioTotal - factura.Descuento),
                Estado = factura.Estado,
                FechaFactura = factura.FechaFactura,
                ClienteFactura = new FacturaClienteDTO
                {
                    IdUsuarioCliente = factura.IdUsuarioCliente,
                    BillingName = factura.BillingName,
                    BillingDocument = factura.BillingDocument,
                    BillingStreet = factura.BillingStreet,
                    BillingCity = factura.BillingCity,
                    BillingProvince = factura.BillingProvince,
                    BillingPostalCode = factura.BillingPostalCode,
                    BillingEmail = factura.BillingEmail,
                    BillingPhone = factura.BillingPhone,
                    EsAnonima = !factura.IdUsuarioCliente.HasValue && factura.BillingName == "Cliente anónimo"
                },
                Lineas = lineas,
                PedidoIds = pedidoIds
            };
        }

        private async Task<UsuarioCliente?> ResolveClienteForFacturaAsync(AsignarFacturaClienteDTO dto, CancellationToken cancellationToken)
        {
            if (dto.IdUsuarioCliente.HasValue)
                return await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.IdUsuarioCliente == dto.IdUsuarioCliente.Value, cancellationToken);


            if (!string.IsNullOrWhiteSpace(dto.Dni))
            {
                var dni = dto.Dni.Trim().ToLower();
                var byDni = await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.Dni.ToLower() == dni, cancellationToken);
                if (byDni != null)
                    return byDni;
            }

            if (string.IsNullOrWhiteSpace(dto.Cif))
                return null;

            var cif = dto.Cif.Trim().ToLower();
            return await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.Cif.ToLower() == cif, cancellationToken);
        }

        private static void UpdateCustomerFiscalData(UsuarioCliente cliente, AsignarFacturaClienteDTO dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.FiscalName))
                cliente.FiscalName = dto.FiscalName.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Dni))
                cliente.Dni = dto.Dni.Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(dto.Cif))
                cliente.Cif = dto.Cif.Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(dto.BillingStreet))
                cliente.BillingStreet = dto.BillingStreet.Trim();

            if (!string.IsNullOrWhiteSpace(dto.BillingCity))
                cliente.BillingCity = dto.BillingCity.Trim();

            if (!string.IsNullOrWhiteSpace(dto.BillingProvince))
                cliente.BillingProvince = dto.BillingProvince.Trim();

            if (!string.IsNullOrWhiteSpace(dto.BillingPostalCode))
                cliente.BillingPostalCode = dto.BillingPostalCode.Trim();

            if (!string.IsNullOrWhiteSpace(dto.BillingPhone))
                cliente.Phone = dto.BillingPhone.Trim();
        }

        private static void ApplyCustomerBillingSnapshot(Factura factura, UsuarioCliente cliente, AsignarFacturaClienteDTO dto)
        {
            factura.IdUsuarioCliente = cliente.IdUsuarioCliente;
            factura.BillingName = ResolveBillingName(cliente, dto);
            factura.BillingDocument = ResolveBillingDocument(cliente, dto);
            factura.BillingStreet = FirstNonEmpty(dto.BillingStreet, cliente.BillingStreet, "Calle Falsa 123");
            factura.BillingCity = FirstNonEmpty(dto.BillingCity, cliente.BillingCity, "Madrid");
            factura.BillingProvince = FirstNonEmpty(dto.BillingProvince, cliente.BillingProvince, "Madrid");
            factura.BillingPostalCode = FirstNonEmpty(dto.BillingPostalCode, cliente.BillingPostalCode, "28000");
            factura.BillingEmail = FirstNonEmpty(dto.BillingEmail, cliente.Email, "anonimo@gestaurante.local");
            factura.BillingPhone = FirstNonEmpty(dto.BillingPhone, cliente.Phone, "600000000");
        }

        private static void ApplyManualBillingSnapshot(Factura factura, AsignarFacturaClienteDTO dto)
        {
            factura.IdUsuarioCliente = null;
            factura.BillingName = FirstNonEmpty(dto.FiscalName, "Cliente anónimo");
            factura.BillingDocument = FirstNonEmpty(dto.Dni, dto.Cif, "00000000X").ToUpperInvariant();
            factura.BillingStreet = FirstNonEmpty(dto.BillingStreet, "Calle Falsa 123");
            factura.BillingCity = FirstNonEmpty(dto.BillingCity, "Madrid");
            factura.BillingProvince = FirstNonEmpty(dto.BillingProvince, "Madrid");
            factura.BillingPostalCode = FirstNonEmpty(dto.BillingPostalCode, "28000");
            factura.BillingEmail = FirstNonEmpty(dto.BillingEmail, "anonimo@gestaurante.local");
            factura.BillingPhone = FirstNonEmpty(dto.BillingPhone, "600000000");
        }

        private static string ResolveBillingName(UsuarioCliente cliente, AsignarFacturaClienteDTO dto)
        {
            return FirstNonEmpty(dto.FiscalName, cliente.FiscalName, $"{cliente.FirstName} {cliente.LastName}".Trim(), "Cliente anónimo");
        }

        private static string ResolveBillingDocument(UsuarioCliente cliente, AsignarFacturaClienteDTO dto)
        {
            return FirstNonEmpty(dto.Dni, dto.Cif, cliente.Dni, cliente.Cif, "00000000X").ToUpperInvariant();
        }

        private static void ApplyAnonymousBillingSnapshot(Factura factura)
        {
            factura.IdUsuarioCliente = null;
            factura.BillingName = "Cliente anónimo";
            factura.BillingDocument = "00000000X";
            factura.BillingStreet = "Calle Falsa 123";
            factura.BillingCity = "Madrid";
            factura.BillingProvince = "Madrid";
            factura.BillingPostalCode = "28000";
            factura.BillingEmail = "anonimo@gestaurante.local";
            factura.BillingPhone = "600000000";
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
        }

        private static string ResolveFacturaEmailTarget(FacturaDTO factura, string? requestedEmail)
        {
            var providedEmail = requestedEmail?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(providedEmail))
                return providedEmail;

            if (factura.ClienteFactura.EsAnonima)
                throw new InvalidOperationException("Debes indicar un email para enviar una factura anónima.");

            if (string.IsNullOrWhiteSpace(factura.ClienteFactura.BillingEmail))
                throw new InvalidOperationException("La factura no tiene un email de destino disponible.");

            return factura.ClienteFactura.BillingEmail.Trim();
        }

        private static string BuildFacturaEmailBody(FacturaDTO factura)
        {
            var lineas = factura.Lineas.Count == 0
                ? "No hay líneas disponibles en esta factura."
                : string.Join(Environment.NewLine, factura.Lineas.Select(linea =>
                    $"- Pedido {linea.IdPedido.ToString()[..8]} · {linea.Cantidad} x {linea.PlatoNombre} · {linea.PrecioUnitario:0.00} EUR · Total {linea.TotalLinea:0.00} EUR"));

            return string.Join(
                Environment.NewLine,
                [
                    "Gestaurante",
                    $"Factura: {factura.NumeroFactura}",
                    $"Fecha: {factura.FechaFactura:dd/MM/yyyy HH:mm}",
                    $"Cliente: {factura.ClienteFactura.BillingName}",
                    $"Canal: {factura.CanalPedido?.ToString() ?? "SALA"}",
                    string.Empty,
                    "Detalle:",
                    lineas,
                    string.Empty,
                    $"Total bruto: {factura.PrecioTotal:0.00} EUR",
                    $"Descuento: {factura.Descuento:0.00} EUR",
                    $"Total final: {factura.TotalConDescuento:0.00} EUR"
                ]);
        }
    }
}
