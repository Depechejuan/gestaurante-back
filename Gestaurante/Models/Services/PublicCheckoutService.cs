using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    /// <summary>
    /// Expone el catálogo público y el checkout autenticado para pedidos online.
    /// </summary>
    public class PublicCheckoutService
    {
        private readonly AppDbContext _db;
        private readonly PedidoService _pedidoService;
        private readonly FacturaService _facturaService;
        private readonly SimulatedPaymentService _simulatedPaymentService;
        private readonly IEmailService _emailService;
        private readonly CatalogProjectionService _catalogProjectionService;

        /// <summary>
        /// Inicializa el servicio público de checkout con las dependencias de pedidos, facturas y catálogo.
        /// </summary>
        /// <param name="db">Contexto EF del dominio.</param>
        /// <param name="pedidoService">Servicio de creación y consulta de pedidos.</param>
        /// <param name="facturaService">Servicio de creación de facturas.</param>
        /// <param name="simulatedPaymentService">Servicio de pago online simulado.</param>
        /// <param name="emailService">Servicio de envío de correos transaccionales.</param>
        /// <param name="catalogProjectionService">Servicio de proyección del catálogo para consumo público.</param>
        public PublicCheckoutService(
            AppDbContext db,
            PedidoService pedidoService,
            FacturaService facturaService,
            SimulatedPaymentService simulatedPaymentService,
            IEmailService emailService,
            CatalogProjectionService catalogProjectionService)
        {
            _db = db;
            _pedidoService = pedidoService;
            _facturaService = facturaService;
            _simulatedPaymentService = simulatedPaymentService;
            _emailService = emailService;
            _catalogProjectionService = catalogProjectionService;
        }

        /// <summary>
        /// Recupera el catálogo público de platos disponibles para compra online.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Lista de platos disponibles y proyectados para cliente público.</returns>
        public async Task<List<PlatoDTO>> GetCatalogoAsync(CancellationToken cancellationToken = default)
        {
            var platos = await _db.Platos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.PlatoIngredientes)
                    .ThenInclude(pi => pi.Ingrediente)
                .Where(p => p.Disponible)
                .OrderBy(p => p.Categoria!.Descripcion)
                .ThenBy(p => p.Nombre)
                .ToListAsync(cancellationToken);

            return platos.Select(_catalogProjectionService.MapPublic).ToList();
        }

        /// <summary>
        /// Recupera un plato público concreto si está disponible para pedido online.
        /// </summary>
        /// <param name="platoId">Identificador del plato solicitado.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Plato proyectado para cliente o <see langword="null"/> si no está disponible.</returns>
        public async Task<PlatoDTO?> GetCatalogoItemAsync(Guid platoId, CancellationToken cancellationToken = default)
        {
            var plato = await _db.Platos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.PlatoIngredientes)
                    .ThenInclude(pi => pi.Ingrediente)
                .FirstOrDefaultAsync(p => p.IdPlato == platoId && p.Disponible, cancellationToken);

            return plato == null ? null : _catalogProjectionService.MapPublic(plato);
        }

        /// <summary>
        /// Crea un pedido online autenticado, calcula envío y genera la factura si el pago es online.
        /// </summary>
        /// <param name="clienteId">Identificador del cliente autenticado.</param>
        /// <param name="dto">Datos de checkout, entrega, pago y líneas del pedido.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Pedido online creado y persistido.</returns>
        /// <remarks>
        /// Para pedidos con pago online se genera también la factura pagada y se envía un correo de confirmación.
        /// </remarks>
        public async Task<PedidoDTO> CreateOnlineOrderAsync(Guid clienteId, CreateOnlineOrderDTO dto, CancellationToken cancellationToken = default)
        {
            var cliente = await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.IdUsuarioCliente == clienteId, cancellationToken)
                ?? throw new KeyNotFoundException("Cliente no encontrado.");

            if (!cliente.EmailVerificado || !cliente.Activo)
                throw new UnauthorizedAccessException("La cuenta del cliente no está activa o validada.");

            if (dto.Detalles.Count == 0)
                throw new ValidationException("El pedido online debe contener al menos una línea.");

            if (dto.TipoEntrega == TipoEntrega.DOMICILIO && !dto.PagarOnline)
                throw new ValidationException("Los pedidos a domicilio requieren pago online.");

            var subtotalProductos = await ResolveSubtotalProductosAsync(dto.Detalles, cancellationToken);
            var gastosEnvio = ResolveGastosEnvio(dto.TipoEntrega, subtotalProductos);

            var direccionSnapshot = string.Empty;
            if (dto.TipoEntrega == TipoEntrega.DOMICILIO)
            {
                if (!dto.IdClienteDireccion.HasValue)
                    throw new ValidationException("Debes seleccionar una dirección de entrega.");

                var direccion = await _db.ClienteDirecciones
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.IdClienteDireccion == dto.IdClienteDireccion.Value && d.IdUsuarioCliente == clienteId, cancellationToken)
                    ?? throw new KeyNotFoundException("Dirección de entrega no encontrada.");

                direccionSnapshot = $"{direccion.Street}, {direccion.PostalCode} {direccion.City}, {direccion.Province}";
            }

            ClienteMetodoPago? metodoPago = null;
            if (dto.PagarOnline)
                metodoPago = await _simulatedPaymentService.ResolvePaymentMethodAsync(
                    clienteId,
                    dto.PaymentMethod ?? new CheckoutPaymentMethodDTO(),
                    cancellationToken);

            var pedido = await _pedidoService.CreateAsync(new CrearPedidoDTO
            {
                IdMesa = null,
                Estado = EstadoPedido.CONFIRMADO,
                CanalPedido = CanalPedido.ONLINE,
                TipoEntrega = dto.TipoEntrega,
                EstadoPago = dto.PagarOnline ? EstadoPago.PAGADO_ONLINE : EstadoPago.PENDIENTE_LOCAL,
                IdUsuarioCliente = clienteId,
                ClienteNombre = $"{cliente.FirstName} {cliente.LastName}".Trim(),
                ClienteEmail = cliente.Email,
                ClienteTelefono = cliente.Phone,
                ClienteDireccionSnapshot = direccionSnapshot,
                GastosEnvio = gastosEnvio,
                Notas = dto.Notas,
                Detalles = dto.Detalles
            }, cancellationToken);

            if (dto.PagarOnline)
            {
                var factura = await _facturaService.CreateAsync(new CrearFacturaDTO
                {
                    IdPedido = pedido.IdPedido,
                    Estado = EstadoFactura.PAGADO,
                    CanalPedido = CanalPedido.ONLINE
                }, cancellationToken);

                await _emailService.SendAsync(
                    cliente.Email,
                    "Factura de tu pedido online",
                    $"Tu pedido online {pedido.IdPedido} ha sido pagado correctamente. Factura: {factura.NumeroFactura}. Total: {factura.TotalConDescuento:0.00} EUR.",
                    cancellationToken: cancellationToken);

                return await _pedidoService.GetByIdAsync(pedido.IdPedido, cancellationToken)
                    ?? pedido;
            }

            return pedido;
        }

        /// <summary>
        /// Calcula el subtotal de los artículos comprobando que todos los platos existen y están disponibles.
        /// </summary>
        /// <param name="detalles">Líneas recibidas en el checkout.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Importe total de artículos antes de gastos de envío.</returns>
        private async Task<double> ResolveSubtotalProductosAsync(List<CrearDetallePedidoDTO> detalles, CancellationToken cancellationToken)
        {
            var detallesValidos = detalles.Where(detalle => detalle.Cantidad > 0).ToList();
            if (detallesValidos.Count == 0)
                return 0;

            var platos = await _db.Platos
                .AsNoTracking()
                .Where(plato => detallesValidos.Select(detalle => detalle.IdPlato).Contains(plato.IdPlato))
                .ToDictionaryAsync(plato => plato.IdPlato, cancellationToken);

            var subtotal = 0d;
            foreach (var detalle in detallesValidos)
            {
                if (!platos.TryGetValue(detalle.IdPlato, out var plato))
                    throw new KeyNotFoundException("Uno de los platos del pedido no existe.");

                if (!plato.Disponible)
                    throw new ValidationException($"El plato {plato.Nombre} no está disponible.");

                subtotal += detalle.Cantidad * Convert.ToDouble(plato.Precio);
            }

            return subtotal;
        }

        /// <summary>
        /// Determina los gastos de envío según el tipo de entrega y el subtotal del pedido.
        /// </summary>
        /// <param name="tipoEntrega">Modalidad de entrega elegida por el cliente.</param>
        /// <param name="subtotalProductos">Subtotal de artículos sin incluir envío.</param>
        /// <returns>Importe de gastos de envío a aplicar al pedido.</returns>
        private static double ResolveGastosEnvio(TipoEntrega tipoEntrega, double subtotalProductos)
        {
            if (tipoEntrega != TipoEntrega.DOMICILIO)
                return 0;

            if (subtotalProductos < 20)
                return 5;

            if (subtotalProductos < 30)
                return 2;

            return 0;
        }
    }
}
