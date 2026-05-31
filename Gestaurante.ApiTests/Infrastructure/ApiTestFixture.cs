using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gestaurante.Configuration;
using Gestaurante.Models.Data;
using Gestaurante.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Gestaurante.ApiTests.Infrastructure;

public sealed class ApiTestFixture : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public TestApiFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    public TestSeedState State { get; private set; } = new();
    public FakeEmailService EmailService => Factory.Services.GetRequiredService<FakeEmailService>();
    public FakePlatoImageService PlatoImageService => Factory.Services.GetRequiredService<FakePlatoImageService>();

    public async Task InitializeAsync()
    {
        AppConfiguration.LoadDotEnv();
        await RecreateTestDatabaseAsync();

        Factory = new TestApiFactory();
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory is not null)
            await Factory.DisposeAsync();
    }

    public async Task ResetAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await EnsureUsingTestDatabaseAsync(db);
        await db.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE
                "AccountActionTokens",
                "ClienteEmailVerifications",
                "ClienteMetodosPago",
                "ClienteDirecciones",
                "MesaPublicSessions",
                "DetallesPedido",
                "Facturas",
                "Pedidos",
                "PlatoIngrediente",
                "Platos",
                "Ingredientes",
                "Categorias",
                "UsuariosCliente",
                "Mesas",
                "Empleados"
            RESTART IDENTITY CASCADE;
            """);

        await DbInitializer.SeedDefaultEmployeesAsync(db);
        await DbInitializer.SeedDefaultCustomersAsync(db);
        State = await SeedCustomDataAsync(db);
        EmailService.Clear();
        PlatoImageService.Clear();
    }

    public async Task<string> LoginEmployeeAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/user/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();
        var envelope = await ReadEnvelopeAsync<TokenEnvelope>(response);
        return envelope.Data?.Token ?? throw new InvalidOperationException("No se recibió token de empleado.");
    }

    public Task<string> LoginAdminAsync()
    {
        return LoginEmployeeAsync(
            "admin@gestaurante.com",
            Environment.GetEnvironmentVariable("DEFAULT_ADMIN_PASSWORD") ?? "Password123!");
    }

    public Task<string> LoginCamareroAsync()
    {
        return LoginEmployeeAsync(
            "paula.garcia@gestaurante.com",
            Environment.GetEnvironmentVariable("DEFAULT_CAMARERO_PASSWORD") ?? "Cmarer0.");
    }

    public Task<string> LoginCocineroAsync()
    {
        return LoginEmployeeAsync(
            "lucas.romero@gestaurante.com",
            Environment.GetEnvironmentVariable("DEFAULT_COCINERO_PASSWORD") ?? "Cociner0.");
    }

    public Task<string> LoginRepartidorAsync()
    {
        return LoginEmployeeAsync(
            "sergio.reparto@gestaurante.com",
            Environment.GetEnvironmentVariable("DEFAULT_REPARTIDOR_PASSWORD") ?? "Repartid0r.");
    }

    public async Task<string> LoginCustomerAsync(string email = "ana.morales@cliente.gestaurante.com")
    {
        var response = await Client.PostAsJsonAsync("/public/account/login", new
        {
            email,
            password = Environment.GetEnvironmentVariable("DEFAULT_CLIENT_PASSWORD") ?? "Client3."
        });

        response.EnsureSuccessStatusCode();
        var envelope = await ReadEnvelopeAsync<CustomerTokenEnvelope>(response);
        return envelope.Data?.Token ?? throw new InvalidOperationException("No se recibió token de cliente.");
    }

    public void SetBearerToken(HttpRequestMessage request, string token)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public HttpRequestMessage CreateRequest(HttpMethod method, string uri, string? token = null)
    {
        var request = new HttpRequestMessage(method, uri);
        if (!string.IsNullOrWhiteSpace(token))
            SetBearerToken(request, token);

        return request;
    }

    public async Task<ApiEnvelope<T>> ReadEnvelopeAsync<T>(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiEnvelope<T>>(payload, JsonOptions)
            ?? throw new InvalidOperationException("No se pudo deserializar la respuesta JSON.");
    }

    public static string ExtractLinkToken(FakeEmailService.SentEmail email, string path)
    {
        var escapedPath = Regex.Escape(path);
        var match = Regex.Match(email.Body, $@"{escapedPath}\?token=([A-Za-z0-9_\-]+)");
        return match.Success
            ? match.Groups[1].Value
            : throw new InvalidOperationException("No se encontró el token en el correo de pruebas.");
    }

    private static async Task RecreateTestDatabaseAsync()
    {
        var host = GetRequiredEnv("DB_HOST");
        var user = GetRequiredEnv("DB_USER");
        var password = GetRequiredEnv("DB_PASSWORD");
        var adminDatabase = GetRequiredEnv("DB_NAME");
        var port = int.TryParse(Environment.GetEnvironmentVariable("DB_PORT"), out var parsedPort) ? parsedPort : 5432;

        if (!TestApiFactory.TestDatabaseName.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Nombre de base de pruebas no permitido: {TestApiFactory.TestDatabaseName}.");

        if (string.Equals(adminDatabase, TestApiFactory.TestDatabaseName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("DB_NAME no puede apuntar a la base de pruebas al recrearla.");

        var adminConnection = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = adminDatabase,
            Username = user,
            Password = password,
            SslMode = SslMode.Require
        };

        await using var connection = new NpgsqlConnection(adminConnection.ConnectionString);
        await connection.OpenAsync();

        NpgsqlConnection.ClearAllPools();
        await using var dropCommand = new NpgsqlCommand($"""drop database if exists "{TestApiFactory.TestDatabaseName}" with (force)""", connection);
        await dropCommand.ExecuteNonQueryAsync();

        await using var createCommand = new NpgsqlCommand($"""create database "{TestApiFactory.TestDatabaseName}" """, connection);
        await createCommand.ExecuteNonQueryAsync();
    }

    private static async Task<TestSeedState> SeedCustomDataAsync(AppDbContext db)
    {
        var categoriaId = Guid.NewGuid();
        var tomateId = Guid.NewGuid();
        var mozzarellaId = Guid.NewGuid();
        var platoCapreseId = Guid.NewGuid();
        var platoPizzaId = Guid.NewGuid();
        var verifiedCustomer = await db.UsuariosCliente.FirstAsync(cliente => cliente.Email == "ana.morales@cliente.gestaurante.com");
        var anonymousCustomer = await db.UsuariosCliente.FirstAsync(cliente => cliente.Email == "anonimo@gestaurante.local");

        var categorias = new[]
        {
            new Categoria(categoriaId, "Entrantes")
        };

        var ingredientes = new[]
        {
            new Ingrediente(tomateId, "Tomate", false, true, string.Empty),
            new Ingrediente(mozzarellaId, "Mozzarella", true, true, string.Empty)
        };

        var platoCaprese = new Plato(platoCapreseId, "Ensalada Caprese", "Tomate, mozzarella y albahaca.", string.Empty, true, 9.50m, categoriaId);
        platoCaprese.PlatoIngredientes.Add(new PlatoIngrediente(platoCapreseId, tomateId));
        platoCaprese.PlatoIngredientes.Add(new PlatoIngrediente(platoCapreseId, mozzarellaId));

        var platoPizza = new Plato(platoPizzaId, "Pizza Margarita", "Pizza clasica con tomate y queso.", string.Empty, true, 12.50m, categoriaId);
        platoPizza.PlatoIngredientes.Add(new PlatoIngrediente(platoPizzaId, tomateId));
        platoPizza.PlatoIngredientes.Add(new PlatoIngrediente(platoPizzaId, mozzarellaId));

        await db.Categorias.AddRangeAsync(categorias);
        await db.Ingredientes.AddRangeAsync(ingredientes);
        await db.Platos.AddRangeAsync(platoCaprese, platoPizza);

        var orderedMesas = await db.Mesas
            .OrderBy(mesa => mesa.Ubicacion)
            .ToListAsync();

        var salaMesa = orderedMesas[0];
        var publicMesa = orderedMesas[1];
        salaMesa.Estado = false;

        var address = new ClienteDireccion
        {
            IdClienteDireccion = Guid.NewGuid(),
            IdUsuarioCliente = verifiedCustomer.IdUsuarioCliente,
            Alias = "Casa",
            Street = "Calle Mayor 1",
            City = "Madrid",
            Province = "Madrid",
            PostalCode = "28001",
            IsDefault = true
        };

        var paymentMethod = new ClienteMetodoPago
        {
            IdClienteMetodoPago = Guid.NewGuid(),
            IdUsuarioCliente = verifiedCustomer.IdUsuarioCliente,
            PaymentToken = "tok_test_saved_card",
            Brand = "VISA",
            Last4 = "4242",
            HolderName = "Ana Morales",
            ExpMonth = 12,
            ExpYear = 2030,
            IsDefault = true
        };

        var salaPedido = new Pedido(
            Guid.NewGuid(),
            salaMesa.IdMesa,
            DateTime.UtcNow.AddMinutes(-45),
            EstadoPedido.PENDIENTE,
            canalPedido: CanalPedido.SALA,
            tipoEntrega: TipoEntrega.MESA,
            estadoPago: EstadoPago.NO_APLICA)
        {
            Notas = "Pedido de sala de pruebas."
        };

        salaPedido.DetallesPedido.Add(new DetallePedido(Guid.NewGuid(), platoCapreseId, salaPedido.IdPedido, 2, 9.50));

        var onlinePedido = new Pedido(
            Guid.NewGuid(),
            null,
            DateTime.UtcNow.AddMinutes(-20),
            EstadoPedido.PENDIENTE_ENTREGA,
            idUsuarioCliente: verifiedCustomer.IdUsuarioCliente,
            canalPedido: CanalPedido.ONLINE,
            tipoEntrega: TipoEntrega.DOMICILIO,
            estadoPago: EstadoPago.PAGADO_ONLINE)
        {
            ClienteNombre = "Ana Morales",
            ClienteEmail = verifiedCustomer.Email,
            ClienteTelefono = "600100101",
            ClienteDireccionSnapshot = "Calle Mayor 1, Madrid",
            GastosEnvio = 5,
            Notas = "Pedido online de prueba."
        };

        onlinePedido.DetallesPedido.Add(new DetallePedido(Guid.NewGuid(), platoPizzaId, onlinePedido.IdPedido, 1, 12.50)
        {
            Estado = EstadoDetallePedido.ENTREGADA
        });

        var facturaSala = BuildAnonymousFactura(
            Guid.NewGuid(),
            salaMesa.IdMesa,
            salaPedido.IdPedido,
            anonymousCustomer.IdUsuarioCliente,
            19.00,
            CanalPedido.SALA);

        var facturaManual = BuildAnonymousFactura(
            Guid.NewGuid(),
            null,
            null,
            anonymousCustomer.IdUsuarioCliente,
            8.50,
            CanalPedido.SALA);

        await db.ClienteDirecciones.AddAsync(address);
        await db.ClienteMetodosPago.AddAsync(paymentMethod);
        await db.Pedidos.AddRangeAsync(salaPedido, onlinePedido);
        await db.SaveChangesAsync();

        await db.Facturas.AddRangeAsync(facturaSala, facturaManual);
        await db.SaveChangesAsync();

        salaPedido.IdFactura = facturaSala.NumeroFactura;
        await db.SaveChangesAsync();

        return new TestSeedState
        {
            CategoriaId = categoriaId,
            IngredienteTomateId = tomateId,
            IngredienteMozzarellaId = mozzarellaId,
            PlatoCapreseId = platoCapreseId,
            PlatoPizzaId = platoPizzaId,
            SalaMesaId = salaMesa.IdMesa,
            PublicMesaId = publicMesa.IdMesa,
            VerifiedCustomerId = verifiedCustomer.IdUsuarioCliente,
            VerifiedCustomerAddressId = address.IdClienteDireccion,
            VerifiedCustomerPaymentMethodId = paymentMethod.IdClienteMetodoPago,
            SalaPedidoId = salaPedido.IdPedido,
            OnlinePedidoId = onlinePedido.IdPedido,
            FacturaSalaId = facturaSala.NumeroFactura,
            FacturaManualId = facturaManual.NumeroFactura,
            AnonymousCustomerId = anonymousCustomer.IdUsuarioCliente
        };
    }

    private static async Task EnsureUsingTestDatabaseAsync(AppDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "select current_database()";

        var currentDatabase = (await command.ExecuteScalarAsync())?.ToString();
        if (!string.Equals(currentDatabase, TestApiFactory.TestDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Las pruebas intentan limpiar la base '{currentDatabase}'. Se esperaba '{TestApiFactory.TestDatabaseName}'.");
        }
    }

    private static Factura BuildAnonymousFactura(Guid id, Guid? mesaId, Guid? pedidoId, Guid anonymousCustomerId, double total, CanalPedido canalPedido)
    {
        return new Factura(id, mesaId, pedidoId, total, 0, EstadoFactura.PENDIENTE, DateTime.UtcNow, canalPedido)
        {
            IdUsuarioCliente = anonymousCustomerId,
            TipoDescuento = TipoDescuentoFactura.FIJO,
            ValorDescuento = 0,
            BillingName = "Cliente anónimo",
            BillingDocument = "00000000X",
            BillingStreet = "Calle Falsa 123",
            BillingCity = "Madrid",
            BillingProvince = "Madrid",
            BillingPostalCode = "28000",
            BillingEmail = "anonimo@gestaurante.local",
            BillingPhone = "600000000"
        };
    }

    private static string GetRequiredEnv(string key)
    {
        return Environment.GetEnvironmentVariable(key)?.Trim('\'', '"')
            ?? throw new InvalidOperationException($"{key} no definido para pruebas.");
    }

    public sealed class ApiEnvelope<T>
    {
        public int Status { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
    }

    public sealed class TokenEnvelope
    {
        public string Token { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
    }

    public sealed class CustomerTokenEnvelope
    {
        public string Token { get; set; } = string.Empty;
        public Guid IdUsuarioCliente { get; set; }
    }
}
