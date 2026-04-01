using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Utils;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class PublicCheckoutService
    {
        private readonly AppDbContext _db;
        private readonly PedidoService _pedidoService;
        private readonly FacturaService _facturaService;
        private readonly MockPaymentService _mockPaymentService;
        private readonly IEmailService _emailService;

        public PublicCheckoutService(
            AppDbContext db,
            PedidoService pedidoService,
            FacturaService facturaService,
            MockPaymentService mockPaymentService,
            IEmailService emailService)
        {
            _db = db;
            _pedidoService = pedidoService;
            _facturaService = facturaService;
            _mockPaymentService = mockPaymentService;
            _emailService = emailService;
        }

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

            return platos.Select(MapPublicPlato).ToList();
        }

        public async Task<PlatoDTO?> GetCatalogoItemAsync(Guid platoId, CancellationToken cancellationToken = default)
        {
            var plato = await _db.Platos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.PlatoIngredientes)
                    .ThenInclude(pi => pi.Ingrediente)
                .FirstOrDefaultAsync(p => p.IdPlato == platoId && p.Disponible, cancellationToken);

            return plato == null ? null : MapPublicPlato(plato);
        }

        public async Task<PedidoDTO> CreateOnlineOrderAsync(Guid clienteId, CreateOnlineOrderDTO dto, CancellationToken cancellationToken = default)
        {
            var cliente = await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.IdUsuarioCliente == clienteId, cancellationToken)
                ?? throw new KeyNotFoundException("Cliente no encontrado.");

            if (!cliente.EmailVerificado || !cliente.Activo)
                throw new UnauthorizedAccessException("La cuenta del cliente no está activa o validada.");

            if (dto.Detalles.Count == 0)
                throw new InvalidOperationException("El pedido online debe contener al menos una línea.");

            if (dto.TipoEntrega == TipoEntrega.DOMICILIO && !dto.PagarOnline)
                throw new InvalidOperationException("Los pedidos a domicilio requieren pago online.");

            var subtotalProductos = await ResolveSubtotalProductosAsync(dto.Detalles, cancellationToken);
            var gastosEnvio = ResolveGastosEnvio(dto.TipoEntrega, subtotalProductos);

            var direccionSnapshot = string.Empty;
            if (dto.TipoEntrega == TipoEntrega.DOMICILIO)
            {
                if (!dto.IdClienteDireccion.HasValue)
                    throw new InvalidOperationException("Debes seleccionar una dirección de entrega.");

                var direccion = await _db.ClienteDirecciones
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.IdClienteDireccion == dto.IdClienteDireccion.Value && d.IdUsuarioCliente == clienteId, cancellationToken)
                    ?? throw new KeyNotFoundException("Dirección de entrega no encontrada.");

                direccionSnapshot = $"{direccion.Alias} · {direccion.Street}, {direccion.PostalCode} {direccion.City}, {direccion.Province}";
            }

            ClienteMetodoPago? metodoPago = null;
            if (dto.PagarOnline)
                metodoPago = await _mockPaymentService.ResolvePaymentMethodAsync(
                    clienteId,
                    dto.PaymentMethod ?? new CheckoutPaymentMethodDTO(),
                    cancellationToken);

            var pedido = await _pedidoService.CreateAsync(new CrearPedidoDTO
            {
                IdMesa = null,
                Estado = EstadoPedido.CONFIRMADO,
                CanalPedido = CanalPedido.ONLINE,
                TipoEntrega = dto.TipoEntrega,
                EstadoPago = dto.PagarOnline ? EstadoPago.PAGADO_MOCK : EstadoPago.PENDIENTE_LOCAL,
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
                    throw new InvalidOperationException($"El plato {plato.Nombre} no está disponible.");

                subtotal += detalle.Cantidad * Convert.ToDouble(plato.Precio);
            }

            return subtotal;
        }

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

        private static PlatoDTO MapPublicPlato(Plato plato)
        {
            var ingredientesRaw = plato.PlatoIngredientes
                .OrderBy(pi => pi.Ingrediente != null ? pi.Ingrediente.Nombre : string.Empty)
                .Select(pi => new PlatoIngredienteDTO
                {
                    IdIngrediente = pi.IdIngrediente,
                    Nombre = pi.Ingrediente != null ? pi.Ingrediente.Nombre : string.Empty
                })
                .ToList();

            var ingredientes = PublicIngredientResolver.ResolveForPublic(ingredientesRaw);

            return new PlatoDTO
            {
                IdPlato = plato.IdPlato,
                Nombre = plato.Nombre,
                Descripcion = plato.Descripcion,
                Imagen = plato.Imagen,
                Disponible = plato.Disponible,
                Precio = plato.Precio,
                IdCategoria = plato.IdCategoria,
                CategoriaDescripcion = plato.Categoria != null ? plato.Categoria.Descripcion : string.Empty,
                Ingredientes = ingredientes,
                Alergenos = AllergenResolver.ResolveFromIngredientes(ingredientes.Select(ingrediente => ingrediente.Nombre))
            };
        }
    }
}
