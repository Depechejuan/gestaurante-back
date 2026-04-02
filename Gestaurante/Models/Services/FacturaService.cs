using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class FacturaService
    {
        private const string AnonymousCustomerEmail = "anonimo@gestaurante.local";
        private const string AnonymousCustomerName = "Cliente anónimo";
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
            factura.TipoDescuento = TipoDescuentoFactura.FIJO;
            factura.ValorDescuento = dto.Descuento;
            factura.MotivoDescuento = dto.MotivoDescuento.Trim();
            RecalculateFacturaTotals(factura);
            await ApplyAnonymousBillingSnapshotAsync(factura, cancellationToken);

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

            var updatesAmountData = dto.PrecioTotal.HasValue
                || dto.TipoDescuento.HasValue
                || dto.ValorDescuento.HasValue
                || dto.Descuento.HasValue;
            if (factura.Estado == EstadoFactura.PAGADO && updatesAmountData)
                throw new InvalidOperationException("No se puede modificar el importe o el descuento de una factura ya cobrada.");

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

            if (dto.TipoDescuento.HasValue)
                factura.TipoDescuento = dto.TipoDescuento.Value;

            if (dto.ValorDescuento.HasValue)
                factura.ValorDescuento = dto.ValorDescuento.Value;
            else if (dto.Descuento.HasValue)
            {
                factura.TipoDescuento = TipoDescuentoFactura.FIJO;
                factura.ValorDescuento = dto.Descuento.Value;
            }

            if (dto.MotivoDescuento != null)
                factura.MotivoDescuento = dto.MotivoDescuento.Trim();

            if (dto.Estado.HasValue)
                factura.Estado = dto.Estado.Value;

            if (dto.FechaFactura.HasValue)
                factura.FechaFactura = dto.FechaFactura.Value;

            RecalculateFacturaTotals(factura);

            await _db.SaveChangesAsync(cancellationToken);
            return await BuildFacturaDtoAsync(factura, cancellationToken);
        }

        public async Task<List<FacturaClienteLookupDTO>> SearchClientesAsync(string? query, CancellationToken cancellationToken = default)
        {
            var term = query?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(term))
                return await BuildAnonymousCustomerLookupAsync(cancellationToken);

            var lowered = term.ToLower();
            return await _db.UsuariosCliente
                .AsNoTracking()
                .Where(u => u.Activo)
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
                    Phone = u.Phone,
                    EsAnonimo = u.Email == AnonymousCustomerEmail
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

        public async Task<FacturaDTO?> ChargeAsync(Guid numeroFactura, CobrarFacturaDTO dto, CancellationToken cancellationToken = default)
        {
            var factura = await _db.Facturas.FirstOrDefaultAsync(f => f.NumeroFactura == numeroFactura, cancellationToken);
            if (factura == null)
                return null;

            if (factura.Estado == EstadoFactura.CANCELADO)
                throw new InvalidOperationException("No se puede cobrar una factura cancelada.");

            if (factura.Estado == EstadoFactura.PAGADO)
                throw new InvalidOperationException("La factura ya está cobrada.");

            RecalculateFacturaTotals(factura);
            var totalFinal = CalculateTotalConDescuento(factura);
            if (totalFinal <= 0)
                throw new InvalidOperationException("La factura no tiene importe pendiente de cobro.");

            factura.MetodoCobro = dto.MetodoPago;
            factura.FechaCobro = DateTime.UtcNow;
            factura.Estado = EstadoFactura.PAGADO;

            if (dto.MetodoPago == MetodoPagoFactura.EFECTIVO)
            {
                if (!dto.ImporteEntregado.HasValue)
                    throw new InvalidOperationException("Debes indicar cuánto entrega el cliente en efectivo.");

                if (dto.ImporteEntregado.Value < totalFinal)
                    throw new InvalidOperationException("El importe entregado no cubre el total de la factura.");

                factura.ImporteEntregado = dto.ImporteEntregado.Value;
                factura.CambioEntregado = dto.ImporteEntregado.Value - totalFinal;
            }
            else
            {
                factura.ImporteEntregado = totalFinal;
                factura.CambioEntregado = 0;
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

            await _emailService.SendAsync(email, subject, body, true, cancellationToken);
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
            factura.TipoDescuento = TipoDescuentoFactura.FIJO;
            factura.ValorDescuento = descuento;
            factura.MotivoDescuento = string.Empty;
            RecalculateFacturaTotals(factura);
            await ApplyAnonymousBillingSnapshotAsync(factura, cancellationToken);

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
                .Where(d => pedidoIds.Contains(d.IdPedido) && d.Estado != EstadoDetallePedido.CANCELADA)
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
            factura.TipoDescuento = TipoDescuentoFactura.FIJO;
            factura.ValorDescuento = descuento;
            factura.MotivoDescuento = string.Empty;
            RecalculateFacturaTotals(factura);
            await ApplyAnonymousBillingSnapshotAsync(factura, cancellationToken);

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            await _db.Facturas.AddAsync(factura, cancellationToken);

            foreach (var pedido in pedidosFacturables)
                pedido.IdFactura = factura.NumeroFactura;

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
                .Where(d => d.IdPedido == pedidoId && d.Estado != EstadoDetallePedido.CANCELADA)
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
                .Where(d => pedidoIds.Contains(d.IdPedido) && d.Estado != EstadoDetallePedido.CANCELADA)
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

            var effectiveFactura = await ResolveFacturaDisplaySnapshotAsync(factura, pedidos, cancellationToken);
            return MapFactura(effectiveFactura, pedidoIds, lineas);
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
                .AnyAsync(d => pedidoIds.Contains(d.IdPedido) && d.Estado != EstadoDetallePedido.CANCELADA, cancellationToken);

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
                TipoDescuento = factura.TipoDescuento,
                ValorDescuento = factura.ValorDescuento,
                MotivoDescuento = factura.MotivoDescuento,
                TotalConDescuento = CalculateTotalConDescuento(factura),
                Estado = factura.Estado,
                FechaFactura = factura.FechaFactura,
                MetodoCobro = factura.MetodoCobro,
                ImporteEntregado = factura.ImporteEntregado,
                CambioEntregado = factura.CambioEntregado,
                FechaCobro = factura.FechaCobro,
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
                    EsAnonima = factura.BillingName == AnonymousCustomerName && factura.BillingEmail == AnonymousCustomerEmail
                },
                Lineas = lineas,
                PedidoIds = pedidoIds
            };
        }

        private async Task<UsuarioCliente?> ResolveClienteForFacturaAsync(AsignarFacturaClienteDTO dto, CancellationToken cancellationToken)
        {
            if (dto.IdUsuarioCliente.HasValue)
                return await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.IdUsuarioCliente == dto.IdUsuarioCliente.Value, cancellationToken);

            if (dto.CreateCustomer)
            {
                var existingByEmail = string.IsNullOrWhiteSpace(dto.BillingEmail)
                    ? null
                    : await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.BillingEmail.ToLower(), cancellationToken);
                if (existingByEmail != null)
                    return existingByEmail;

                var fiscalName = FirstNonEmpty(dto.FiscalName, AnonymousCustomerName);
                var parts = fiscalName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var cliente = new UsuarioCliente
                {
                    IdUsuarioCliente = Guid.NewGuid(),
                    Email = FirstNonEmpty(dto.BillingEmail, $"cliente-{Guid.NewGuid():N}@gestaurante.local"),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                    FirstName = parts.FirstOrDefault() ?? "Cliente",
                    LastName = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty,
                    Phone = dto.BillingPhone.Trim(),
                    FiscalName = fiscalName,
                    Dni = dto.Dni.Trim().ToUpperInvariant(),
                    Cif = dto.Cif.Trim().ToUpperInvariant(),
                    BillingStreet = dto.BillingStreet.Trim(),
                    BillingCity = dto.BillingCity.Trim(),
                    BillingProvince = dto.BillingProvince.Trim(),
                    BillingPostalCode = dto.BillingPostalCode.Trim(),
                    Activo = true,
                    EmailVerificado = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _db.UsuariosCliente.AddAsync(cliente, cancellationToken);
                return cliente;
            }

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
            if (IsAnonymousCustomer(cliente))
            {
                factura.IdUsuarioCliente = cliente.IdUsuarioCliente;
                factura.BillingName = AnonymousCustomerName;
                factura.BillingDocument = "00000000X";
                factura.BillingStreet = "Calle Falsa 123";
                factura.BillingCity = "Madrid";
                factura.BillingProvince = "Madrid";
                factura.BillingPostalCode = "28000";
                factura.BillingEmail = AnonymousCustomerEmail;
                factura.BillingPhone = "600000000";
                return;
            }

            factura.IdUsuarioCliente = cliente.IdUsuarioCliente;
            factura.BillingName = ResolveBillingName(cliente, dto);
            factura.BillingDocument = ResolveBillingDocument(cliente, dto);
            factura.BillingStreet = FirstNonEmpty(dto.BillingStreet, cliente.BillingStreet, "Calle Falsa 123");
            factura.BillingCity = FirstNonEmpty(dto.BillingCity, cliente.BillingCity, "Madrid");
            factura.BillingProvince = FirstNonEmpty(dto.BillingProvince, cliente.BillingProvince, "Madrid");
            factura.BillingPostalCode = FirstNonEmpty(dto.BillingPostalCode, cliente.BillingPostalCode, "28000");
            factura.BillingEmail = FirstNonEmpty(dto.BillingEmail, cliente.Email, AnonymousCustomerEmail);
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
            factura.BillingEmail = FirstNonEmpty(dto.BillingEmail, AnonymousCustomerEmail);
            factura.BillingPhone = FirstNonEmpty(dto.BillingPhone, "600000000");
        }

        private async Task ApplyDefaultBillingSnapshotAsync(Factura factura, Pedido? pedido, CancellationToken cancellationToken)
        {
            if (pedido?.IdUsuarioCliente.HasValue == true)
            {
                var cliente = await _db.UsuariosCliente
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.IdUsuarioCliente == pedido.IdUsuarioCliente.Value, cancellationToken);

                if (cliente != null && !IsAnonymousCustomer(cliente))
                {
                    ApplyPedidoCustomerBillingSnapshot(factura, cliente, pedido);
                    return;
                }
            }

            await ApplyAnonymousBillingSnapshotAsync(factura, cancellationToken);
        }

        private static string ResolveBillingName(UsuarioCliente cliente, AsignarFacturaClienteDTO dto)
        {
            return FirstNonEmpty(dto.FiscalName, cliente.FiscalName, $"{cliente.FirstName} {cliente.LastName}".Trim(), "Cliente anónimo");
        }

        private static string ResolveBillingDocument(UsuarioCliente cliente, AsignarFacturaClienteDTO dto)
        {
            return FirstNonEmpty(dto.Dni, dto.Cif, cliente.Dni, cliente.Cif, "00000000X").ToUpperInvariant();
        }

        private async Task ApplyAnonymousBillingSnapshotAsync(Factura factura, CancellationToken cancellationToken)
        {
            factura.IdUsuarioCliente = await _db.UsuariosCliente
                .AsNoTracking()
                .Where(cliente => cliente.Email == AnonymousCustomerEmail)
                .Select(cliente => (Guid?)cliente.IdUsuarioCliente)
                .FirstOrDefaultAsync(cancellationToken);
            factura.BillingName = AnonymousCustomerName;
            factura.BillingDocument = "00000000X";
            factura.BillingStreet = "Calle Falsa 123";
            factura.BillingCity = "Madrid";
            factura.BillingProvince = "Madrid";
            factura.BillingPostalCode = "28000";
            factura.BillingEmail = AnonymousCustomerEmail;
            factura.BillingPhone = "600000000";
        }

        private static void ApplyPedidoCustomerBillingSnapshot(Factura factura, UsuarioCliente cliente, Pedido pedido)
        {
            factura.IdUsuarioCliente = cliente.IdUsuarioCliente;
            factura.BillingName = LimitLength(FirstNonEmpty(
                cliente.FiscalName,
                pedido.ClienteNombre,
                $"{cliente.FirstName} {cliente.LastName}".Trim(),
                cliente.Email,
                "Cliente"), 160);
            factura.BillingDocument = LimitLength(FirstNonEmpty(cliente.Dni, cliente.Cif).ToUpperInvariant(), 20);
            factura.BillingStreet = LimitLength(FirstNonEmpty(
                cliente.BillingStreet,
                pedido.ClienteDireccionSnapshot,
                "Pendiente de completar"), 200);
            factura.BillingCity = LimitLength(FirstNonEmpty(cliente.BillingCity, "Pendiente"), 120);
            factura.BillingProvince = LimitLength(FirstNonEmpty(cliente.BillingProvince, "Pendiente"), 120);
            factura.BillingPostalCode = LimitLength(FirstNonEmpty(cliente.BillingPostalCode, "Pendiente"), 20);
            factura.BillingEmail = LimitLength(FirstNonEmpty(cliente.Email, pedido.ClienteEmail, AnonymousCustomerEmail), 100);
            factura.BillingPhone = LimitLength(FirstNonEmpty(cliente.Phone, pedido.ClienteTelefono, "600000000"), 25);
        }

        private async Task<List<FacturaClienteLookupDTO>> BuildAnonymousCustomerLookupAsync(CancellationToken cancellationToken)
        {
            return await _db.UsuariosCliente
                .AsNoTracking()
                .Where(cliente => cliente.Email == AnonymousCustomerEmail)
                .Select(cliente => new FacturaClienteLookupDTO
                {
                    IdUsuarioCliente = cliente.IdUsuarioCliente,
                    Email = cliente.Email,
                    FullName = $"{cliente.FirstName} {cliente.LastName}".Trim(),
                    FiscalName = cliente.FiscalName,
                    Dni = cliente.Dni,
                    Cif = cliente.Cif,
                    BillingStreet = cliente.BillingStreet,
                    BillingCity = cliente.BillingCity,
                    BillingProvince = cliente.BillingProvince,
                    BillingPostalCode = cliente.BillingPostalCode,
                    Phone = cliente.Phone,
                    EsAnonimo = true
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<Factura> ResolveFacturaDisplaySnapshotAsync(Factura factura, List<Pedido> pedidos, CancellationToken cancellationToken)
        {
            var isAnonymousFactura = factura.BillingName == AnonymousCustomerName && factura.BillingEmail == AnonymousCustomerEmail;
            if (!isAnonymousFactura)
                return factura;

            var pedidoConCliente = pedidos.FirstOrDefault(pedido => pedido.IdUsuarioCliente.HasValue);
            if (pedidoConCliente?.IdUsuarioCliente is not Guid clienteId)
                return factura;

            var cliente = await _db.UsuariosCliente
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUsuarioCliente == clienteId, cancellationToken);
            if (cliente == null || IsAnonymousCustomer(cliente))
                return factura;

            var displayFactura = CloneFactura(factura);
            ApplyPedidoCustomerBillingSnapshot(displayFactura, cliente, pedidoConCliente);
            return displayFactura;
        }

        private static bool IsAnonymousCustomer(UsuarioCliente cliente)
        {
            return cliente.Email == AnonymousCustomerEmail || cliente.FiscalName == AnonymousCustomerName;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
        }

        private static string LimitLength(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value[..maxLength].TrimEnd();
        }

        private static Factura CloneFactura(Factura factura)
        {
            return new Factura(
                factura.NumeroFactura,
                factura.IdMesa,
                factura.IdPedido,
                factura.PrecioTotal,
                factura.Descuento,
                factura.Estado,
                factura.FechaFactura,
                factura.CanalPedido)
            {
                IdUsuarioCliente = factura.IdUsuarioCliente,
                TipoDescuento = factura.TipoDescuento,
                ValorDescuento = factura.ValorDescuento,
                MotivoDescuento = factura.MotivoDescuento,
                MetodoCobro = factura.MetodoCobro,
                ImporteEntregado = factura.ImporteEntregado,
                CambioEntregado = factura.CambioEntregado,
                FechaCobro = factura.FechaCobro,
                BillingName = factura.BillingName,
                BillingDocument = factura.BillingDocument,
                BillingStreet = factura.BillingStreet,
                BillingCity = factura.BillingCity,
                BillingProvince = factura.BillingProvince,
                BillingPostalCode = factura.BillingPostalCode,
                BillingEmail = factura.BillingEmail,
                BillingPhone = factura.BillingPhone
            };
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
            var rows = factura.Lineas.Count == 0
                ? "<tr><td colspan=\"5\" style=\"padding:12px;border-bottom:1px solid #d7e0dc;\">No hay líneas disponibles en esta factura.</td></tr>"
                : string.Join(string.Empty, factura.Lineas.Select(linea =>
                    $"<tr><td style=\"padding:12px;border-bottom:1px solid #d7e0dc;\">{linea.IdPedido.ToString()[..8]}</td><td style=\"padding:12px;border-bottom:1px solid #d7e0dc;\">{linea.PlatoNombre}</td><td style=\"padding:12px;border-bottom:1px solid #d7e0dc;\">{linea.Cantidad}</td><td style=\"padding:12px;border-bottom:1px solid #d7e0dc;\">{linea.PrecioUnitario:0.00} EUR</td><td style=\"padding:12px;border-bottom:1px solid #d7e0dc;\">{linea.TotalLinea:0.00} EUR</td></tr>"));

            var discountReason = string.IsNullOrWhiteSpace(factura.MotivoDescuento)
                ? string.Empty
                : $"<p style=\"margin:0;\">Motivo descuento: <strong>{factura.MotivoDescuento}</strong></p>";

            var customerBlock = factura.ClienteFactura.EsAnonima
                ? $"<p style=\"margin:0;\">{factura.ClienteFactura.BillingName}</p>"
                : $"""
                    <p style="margin:0;">{factura.ClienteFactura.BillingName}</p>
                    <p style="margin:0;">{factura.ClienteFactura.BillingDocument}</p>
                    <p style="margin:0;">{factura.ClienteFactura.BillingStreet}</p>
                    <p style="margin:0;">{factura.ClienteFactura.BillingPostalCode} · {factura.ClienteFactura.BillingCity}</p>
                    <p style="margin:0;">{factura.ClienteFactura.BillingProvince}</p>
                    <p style="margin:0;">{factura.ClienteFactura.BillingEmail}</p>
                    <p style="margin:0;">{factura.ClienteFactura.BillingPhone}</p>
                    """;

            return $"""
                <section style="font-family:Arial,sans-serif;color:#12342b;max-width:920px;margin:0 auto;padding:24px;background:#ffffff;">
                  <header style="display:flex;justify-content:space-between;gap:24px;padding-bottom:16px;border-bottom:1px solid #d7e0dc;">
                    <div>
                      <p style="margin:0 0 6px;text-transform:uppercase;letter-spacing:.18em;font-size:12px;color:#557268;">Gestaurante</p>
                      <h2 style="margin:0 0 8px;">Factura simplificada</h2>
                      <p style="margin:0;">C/ Servicio 17 · 28000 Madrid</p>
                      <p style="margin:0;">gestaurante@local.test · +34 910 000 000</p>
                    </div>
                    <div style="text-align:right;">
                      <p style="margin:0 0 8px;">Factura: <strong>{factura.NumeroFactura}</strong></p>
                      <p style="margin:0 0 8px;">Fecha: <strong>{factura.FechaFactura:dd/MM/yyyy HH:mm}</strong></p>
                      <p style="margin:0;">Canal: <strong>{factura.CanalPedido?.ToString() ?? "SALA"}</strong></p>
                    </div>
                  </header>
                  <section style="display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px;margin-top:18px;">
                    <div style="padding:16px;border:1px solid #d7e0dc;border-radius:16px;background:#f9fcfa;">
                      <h3 style="margin:0 0 12px;">Datos de factura</h3>
                      <p style="margin:0;">Pedidos incluidos: <strong>{factura.PedidoIds.Count}</strong></p>
                    </div>
                    <div style="padding:16px;border:1px solid #d7e0dc;border-radius:16px;background:#f9fcfa;">
                      <h3 style="margin:0 0 12px;">Cliente</h3>
                      {customerBlock}
                    </div>
                  </section>
                  <table style="width:100%;border-collapse:collapse;margin-top:18px;">
                    <thead>
                      <tr>
                        <th style="padding:12px;border-bottom:1px solid #d7e0dc;text-align:left;color:#557268;">Pedido</th>
                        <th style="padding:12px;border-bottom:1px solid #d7e0dc;text-align:left;color:#557268;">Artículo</th>
                        <th style="padding:12px;border-bottom:1px solid #d7e0dc;text-align:left;color:#557268;">Cantidad</th>
                        <th style="padding:12px;border-bottom:1px solid #d7e0dc;text-align:left;color:#557268;">Precio</th>
                        <th style="padding:12px;border-bottom:1px solid #d7e0dc;text-align:left;color:#557268;">Total</th>
                      </tr>
                    </thead>
                    <tbody>
                      {rows}
                    </tbody>
                  </table>
                  <footer style="display:flex;flex-direction:column;gap:8px;align-items:flex-end;margin-top:18px;padding-top:12px;border-top:1px solid #d7e0dc;">
                    <p style="margin:0;">Total bruto: <strong>{factura.PrecioTotal:0.00} EUR</strong></p>
                    <p style="margin:0;">Descuento: <strong>{factura.Descuento:0.00} EUR</strong></p>
                    {discountReason}
                    <p style="margin:0;">Total final: <strong>{factura.TotalConDescuento:0.00} EUR</strong></p>
                  </footer>
                </section>
                """;
        }

        private static void RecalculateFacturaTotals(Factura factura)
        {
            factura.ValorDescuento = Math.Max(0, factura.ValorDescuento);
            factura.Descuento = CalculateDiscountAmount(factura.PrecioTotal, factura.TipoDescuento, factura.ValorDescuento);
        }

        private static double CalculateDiscountAmount(double subtotal, TipoDescuentoFactura tipoDescuento, double valorDescuento)
        {
            if (subtotal <= 0 || valorDescuento <= 0)
                return 0;

            var discount = tipoDescuento == TipoDescuentoFactura.PORCENTAJE
                ? subtotal * (valorDescuento / 100d)
                : valorDescuento;

            return Math.Min(subtotal, Math.Max(0, Math.Round(discount, 2)));
        }

        private static double CalculateTotalConDescuento(Factura factura)
        {
            return Math.Max(0, Math.Round(factura.PrecioTotal - factura.Descuento, 2));
        }
    }
}
