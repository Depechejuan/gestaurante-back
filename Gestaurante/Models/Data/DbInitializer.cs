using BCrypt.Net;
using Gestaurante.Configuration;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gestaurante.Models.Data
{
    public static class DbInitializer
    {
        private const string AnonymousCustomerEmail = "anonimo@gestaurante.local";
        private const string AnonymousCustomerName = "Cliente anónimo";

        public static async Task SeedDefaultEmployeesAsync(AppDbContext context, CancellationToken cancellationToken = default)
        {
            var defaultCredentials = GetDefaultEmployeeCredentials();

            if (await context.Empleados.AnyAsync(cancellationToken))
            {
                await EnsureDefaultEmployeesAsync(context, defaultCredentials, cancellationToken);
                await SeedDefaultMesasAsync(context, cancellationToken);
                return;
            }
            var empleados = defaultCredentials.Select(BuildEmployee).ToList();
            await context.Empleados.AddRangeAsync(empleados, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await SeedDefaultMesasAsync(context, cancellationToken);
        }

        private static async Task SeedDefaultMesasAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            if (await context.Mesas.AnyAsync(cancellationToken))
                return;

            var mesas = new List<Mesa>
            {
                new Mesa(Guid.NewGuid(), 2, true, "Terraza A1"),
                new Mesa(Guid.NewGuid(), 2, true, "Terraza A2"),
                new Mesa(Guid.NewGuid(), 2, true, "Terraza A3"),
                new Mesa(Guid.NewGuid(), 4, true, "Interior B1"),
                new Mesa(Guid.NewGuid(), 4, true, "Interior B2"),
                new Mesa(Guid.NewGuid(), 4, true, "Interior B3"),
                new Mesa(Guid.NewGuid(), 4, true, "Interior B4"),
                new Mesa(Guid.NewGuid(), 6, true, "Salon C1"),
                new Mesa(Guid.NewGuid(), 6, true, "Salon C2"),
                new Mesa(Guid.NewGuid(), 6, true, "Salon C3"),
                new Mesa(Guid.NewGuid(), 8, true, "Reservado D1"),
                new Mesa(Guid.NewGuid(), 8, true, "Reservado D2"),
                new Mesa(Guid.NewGuid(), 10, true, "Salon Grande E1"),
                new Mesa(Guid.NewGuid(), 10, true, "Salon Grande E2"),
                new Mesa(Guid.NewGuid(), 12, true, "Evento F1")
            };

            await context.Mesas.AddRangeAsync(mesas, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        public static async Task CleanupOrphanFacturasAsync(AppDbContext context, CancellationToken cancellationToken = default)
        {
            var facturas = await context.Facturas.ToListAsync(cancellationToken);
            if (facturas.Count == 0)
                return;

            var facturaIds = facturas.Select(f => f.NumeroFactura).ToList();
            var facturasConPedidos = await context.Pedidos
                .AsNoTracking()
                .Where(p => p.IdFactura.HasValue && facturaIds.Contains(p.IdFactura.Value))
                .Select(p => p.IdFactura!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            var facturasHuerfanas = facturas
                .Where(f => !facturasConPedidos.Contains(f.NumeroFactura) && !f.IdPedido.HasValue)
                .ToList();

            if (facturasHuerfanas.Count == 0)
                return;

            context.Facturas.RemoveRange(facturasHuerfanas);
            await context.SaveChangesAsync(cancellationToken);
        }

        public static async Task SeedDefaultCustomersAsync(AppDbContext context, CancellationToken cancellationToken = default)
        {
            string clientPassword = Environment.GetEnvironmentVariable("DEFAULT_CLIENT_PASSWORD")
                ?? throw new InvalidOperationException("DEFAULT_CLIENT_PASSWORD no definido");

            string clientHash = BCrypt.Net.BCrypt.HashPassword(clientPassword, BCrypt.Net.BCrypt.GenerateSalt(12));

            await EnsureAnonymousCustomerAsync(context, clientHash, cancellationToken);

            var defaultCustomers = new List<DefaultCustomerSeed>
            {
                new("ana.morales@cliente.gestaurante.com", "Ana", "Morales Vega", "600100101", "11111111H", ""),
                new("carlos.ruiz@cliente.gestaurante.com", "Carlos", "Ruiz Navarro", "600100102", "11111112J", ""),
                new("laura.santos@cliente.gestaurante.com", "Laura", "Santos Molina", "600100103", "11111113Z", ""),
                new("javier.ortega@cliente.gestaurante.com", "Javier", "Ortega Ramos", "600100104", "11111114S", ""),
                new("elena.cano@cliente.gestaurante.com", "Elena", "Cano Flores", "600100105", "11111115Q", ""),
                new("marta.gil@cliente.gestaurante.com", "Marta", "Gil Prieto", "600100106", "11111116V", ""),
                new("sergio.lopez@cliente.gestaurante.com", "Sergio", "Lopez Campos", "600100107", "11111117N", ""),
                new("irene.perez@cliente.gestaurante.com", "Irene", "Perez Duarte", "600100108", "11111118L", ""),
                new("diego.herrero@cliente.gestaurante.com", "Diego", "Herrero Sanz", "600100109", "11111119C", ""),
                new("lucia.martin@cliente.gestaurante.com", "Lucia", "Martin Bravo", "600100110", "11111120K", "")
            };

            var existingByEmail = await context.UsuariosCliente
                .ToDictionaryAsync(cliente => cliente.Email, StringComparer.OrdinalIgnoreCase, cancellationToken);

            foreach (var seed in defaultCustomers)
            {
                if (existingByEmail.TryGetValue(seed.Email, out var cliente))
                {
                    cliente.PasswordHash = clientHash;
                    cliente.FirstName = seed.FirstName;
                    cliente.LastName = seed.LastName;
                    cliente.Phone = seed.Phone;
                    cliente.Dni = seed.Dni;
                    cliente.Cif = seed.Cif;
                    cliente.FiscalName = $"{seed.FirstName} {seed.LastName}".Trim();
                    cliente.Activo = true;
                    cliente.EmailVerificado = true;
                    cliente.UpdatedAt = DateTime.UtcNow;
                    continue;
                }

                await context.UsuariosCliente.AddAsync(new UsuarioCliente
                {
                    IdUsuarioCliente = Guid.NewGuid(),
                    Email = seed.Email,
                    PasswordHash = clientHash,
                    FirstName = seed.FirstName,
                    LastName = seed.LastName,
                    Phone = seed.Phone,
                    Dni = seed.Dni,
                    Cif = seed.Cif,
                    FiscalName = $"{seed.FirstName} {seed.LastName}".Trim(),
                    BillingStreet = string.Empty,
                    BillingCity = string.Empty,
                    BillingProvince = string.Empty,
                    BillingPostalCode = string.Empty,
                    Activo = true,
                    EmailVerificado = true,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task EnsureAnonymousCustomerAsync(AppDbContext context, string clientHash, CancellationToken cancellationToken)
        {
            var anonymousCustomer = await context.UsuariosCliente
                .FirstOrDefaultAsync(cliente => cliente.Email == AnonymousCustomerEmail, cancellationToken);

            if (anonymousCustomer == null)
            {
                await context.UsuariosCliente.AddAsync(new UsuarioCliente
                {
                    IdUsuarioCliente = Guid.NewGuid(),
                    Email = AnonymousCustomerEmail,
                    PasswordHash = clientHash,
                    FirstName = "Cliente",
                    LastName = "anónimo",
                    Phone = "600000000",
                    FiscalName = AnonymousCustomerName,
                    Dni = "00000000X",
                    Cif = string.Empty,
                    BillingStreet = "Calle Falsa 123",
                    BillingCity = "Madrid",
                    BillingProvince = "Madrid",
                    BillingPostalCode = "28000",
                    Activo = false,
                    EmailVerificado = true,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            anonymousCustomer.PasswordHash = clientHash;
            anonymousCustomer.FirstName = "Cliente";
            anonymousCustomer.LastName = "anónimo";
            anonymousCustomer.Phone = "600000000";
            anonymousCustomer.FiscalName = AnonymousCustomerName;
            anonymousCustomer.Dni = "00000000X";
            anonymousCustomer.Cif = string.Empty;
            anonymousCustomer.BillingStreet = "Calle Falsa 123";
            anonymousCustomer.BillingCity = "Madrid";
            anonymousCustomer.BillingProvince = "Madrid";
            anonymousCustomer.BillingPostalCode = "28000";
            anonymousCustomer.Activo = false;
            anonymousCustomer.EmailVerificado = true;
            anonymousCustomer.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
        }

        private static List<DefaultEmployeeSeed> GetDefaultEmployeeCredentials()
        {
            string adminPassword = Environment.GetEnvironmentVariable("DEFAULT_ADMIN_PASSWORD")
                ?? throw new InvalidOperationException("DEFAULT_ADMIN_PASSWORD no definido");
            string camareroPassword = Environment.GetEnvironmentVariable("DEFAULT_CAMARERO_PASSWORD")
                ?? throw new InvalidOperationException("DEFAULT_CAMARERO_PASSWORD no definido");
            string cocineroPassword = Environment.GetEnvironmentVariable("DEFAULT_COCINERO_PASSWORD")
                ?? throw new InvalidOperationException("DEFAULT_COCINERO_PASSWORD no definido");
            string repartidorPassword = Environment.GetEnvironmentVariable("DEFAULT_REPARTIDOR_PASSWORD")
                ?? camareroPassword;

            return new List<DefaultEmployeeSeed>
            {
                new(
                    "admin@gestaurante.com",
                    adminPassword,
                    TipoEmpleado.Administrador,
                    "Admin",
                    "Gestaurante",
                    "Principal",
                    "00000000T",
                    "0111111111111"
                ),
                new("lucas.romero@gestaurante.com", cocineroPassword, TipoEmpleado.Cocinero, "Lucas", "Romero", "Santos", "00000001R", "0222222222221"),
                new("maria.santos@gestaurante.com", cocineroPassword, TipoEmpleado.Cocinero, "Maria", "Santos", "Ruiz", "00000002W", "0222222222222"),
                new("alberto.molina@gestaurante.com", cocineroPassword, TipoEmpleado.Cocinero, "Alberto", "Molina", "Perez", "00000003A", "0222222222223"),
                new("natalia.ramos@gestaurante.com", cocineroPassword, TipoEmpleado.Cocinero, "Natalia", "Ramos", "Lopez", "00000004G", "0222222222224"),
                new("carmen.navarro@gestaurante.com", cocineroPassword, TipoEmpleado.Cocinero, "Carmen", "Navarro", "Diaz", "00000005M", "0222222222225"),
                new("paula.garcia@gestaurante.com", camareroPassword, TipoEmpleado.Camarero, "Paula", "Garcia", "Martin", "00000006Y", "0333333333331"),
                new("diego.herrera@gestaurante.com", camareroPassword, TipoEmpleado.Camarero, "Diego", "Herrera", "Gil", "00000007F", "0333333333332"),
                new("laura.perez@gestaurante.com", camareroPassword, TipoEmpleado.Camarero, "Laura", "Perez", "Vega", "00000008P", "0333333333333"),
                new("jorge.ruiz@gestaurante.com", camareroPassword, TipoEmpleado.Camarero, "Jorge", "Ruiz", "Ortega", "00000009D", "0333333333334"),
                new("elena.flores@gestaurante.com", camareroPassword, TipoEmpleado.Camarero, "Elena", "Flores", "Cano", "00000010X", "0333333333335"),
                new("sergio.reparto@gestaurante.com", repartidorPassword, TipoEmpleado.Repartidor, "Sergio", "Morales", "Cruz", "00000011B", "0444444444441"),
                new("irene.reparto@gestaurante.com", repartidorPassword, TipoEmpleado.Repartidor, "Irene", "Campos", "Sanz", "00000012N", "0444444444442"),
                new("marcos.reparto@gestaurante.com", repartidorPassword, TipoEmpleado.Repartidor, "Marcos", "Delgado", "Prieto", "00000013J", "0444444444443")
            };
        }

        private static Empleado BuildEmployee(DefaultEmployeeSeed seed)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(seed.Password, BCrypt.Net.BCrypt.GenerateSalt(12));

            Empleado empleado = seed.Tipo switch
            {
                TipoEmpleado.Administrador => new Administrador(seed.Email, passwordHash, seed.FirstName, seed.FirstLastName, seed.SecondLastName, seed.Dni, seed.Nuss),
                TipoEmpleado.Camarero => new Camarero(seed.Email, passwordHash, seed.FirstName, seed.FirstLastName, seed.SecondLastName, seed.Dni, seed.Nuss),
                TipoEmpleado.Repartidor => new Repartidor(seed.Email, passwordHash, seed.FirstName, seed.FirstLastName, seed.SecondLastName, seed.Dni, seed.Nuss),
                _ => new Cocinero(seed.Email, passwordHash, seed.FirstName, seed.FirstLastName, seed.SecondLastName, seed.Dni, seed.Nuss)
            };

            empleado.Activo = true;
            empleado.ImageURL = string.Empty;
            return empleado;
        }

        private static async Task EnsureDefaultEmployeesAsync(
            AppDbContext context,
            IReadOnlyCollection<DefaultEmployeeSeed> defaultEmployees,
            CancellationToken cancellationToken)
        {
            var existingEmployees = await context.Empleados.ToListAsync(cancellationToken);
            var existingByEmail = existingEmployees.ToDictionary(e => e.Email, StringComparer.OrdinalIgnoreCase);

            foreach (var seed in defaultEmployees)
            {
                if (existingByEmail.TryGetValue(seed.Email, out var empleado))
                {
                    empleado.Password = BCrypt.Net.BCrypt.HashPassword(seed.Password, BCrypt.Net.BCrypt.GenerateSalt(12));
                    empleado.Activo = true;
                    empleado.FirstName = seed.FirstName;
                    empleado.FirstLastName = seed.FirstLastName;
                    empleado.SecondLastName = seed.SecondLastName;
                    empleado.UpdatedAt = DateTime.UtcNow;
                    continue;
                }

                await context.Empleados.AddAsync(BuildEmployee(seed), cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private sealed record DefaultEmployeeSeed(
            string Email,
            string Password,
            TipoEmpleado Tipo,
            string FirstName,
            string FirstLastName,
            string SecondLastName,
            string Dni,
            string Nuss
        );

        private sealed record DefaultCustomerSeed(
            string Email,
            string FirstName,
            string LastName,
            string Phone,
            string Dni,
            string Cif
        );
    }
}
