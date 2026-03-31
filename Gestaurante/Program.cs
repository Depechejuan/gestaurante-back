using Gestaurante.Models.Data;
using Gestaurante.Models.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

// Cargar variables del .env
var envCandidates = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), "Gestaurante", ".env"),
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.env"))
};

foreach (var envPath in envCandidates.Distinct())
{
    if (!File.Exists(envPath))
        continue;

    Env.Load(envPath);
}

static string? ReadEnv(string key)
{
    var value = Environment.GetEnvironmentVariable(key)?.Trim();
    if (string.IsNullOrWhiteSpace(value))
        return value;

    return value.Trim().Trim('"');
}

// Creación de la Connection String
string dbHost = ReadEnv("DB_HOST")
    ?? throw new Exception("DB_HOST no definido");

string dbPort = ReadEnv("DB_PORT") ?? "3306";
string dbName = ReadEnv("DB_NAME")
    ?? throw new Exception("DB_NAME no definido");

string dbUser = ReadEnv("DB_USER")
    ?? throw new Exception("DB_USER no definido");

string dbPassword = ReadEnv("DB_PASSWORD")
    ?? throw new Exception("DB_PASSWORD no definido");


string connectionString =
    $"Server={dbHost};Port={dbPort};Database={dbName};User Id={dbUser};Password={dbPassword};SSL Mode=Require;Trust Server Certificate=true;";

string appPort = ReadEnv("PORT") ?? "3000";

Console.WriteLine($"Gestaurante API iniciando en localhost:{appPort}");

var contentRoot = Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Gestaurante"))
    ? Path.Combine(Directory.GetCurrentDirectory(), "Gestaurante")
    : Directory.GetCurrentDirectory();

Console.WriteLine("Creando host web...");

var host = new WebHostBuilder()
    .UseKestrel()
    .UseContentRoot(contentRoot)
    .UseUrls($"http://localhost:{appPort}")
    .ConfigureAppConfiguration((context, configuration) =>
    {
        configuration.SetBasePath(contentRoot);
        configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        configuration.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
        configuration.AddEnvironmentVariables();
    })
    .ConfigureServices(services =>
    {
        services.AddCors(options =>
        {
            options.AddPolicy("LocalPolicy", policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                    {
                        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                            return false;

                        var isLocalHost = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                            || uri.Host.Equals("127.0.0.1");

                        return isLocalHost && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
                    })
                    .AllowAnyHeader()
                    .WithMethods("PUT", "PATCH", "POST", "GET", "DELETE");
            });
        });

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = ReadEnv("JWT_ISSUER"),
                    ValidAudience = ReadEnv("JWT_AUDIENCE"),
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            ReadEnv("JWT_KEY")
                            ?? throw new Exception("JWT_KEY no definida")
                        )
                    ),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var subject = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                            ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var email = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Email)
                            ?? context.Principal?.FindFirstValue(ClaimTypes.Email);

                        if (!Guid.TryParse(subject, out var userId) || string.IsNullOrWhiteSpace(email))
                        {
                            context.Fail("Token inválido.");
                            return;
                        }

                        var empleado = await db.Empleados.FirstOrDefaultAsync(e => e.Id == userId);
                        if (empleado == null || !empleado.Activo || !string.Equals(empleado.Email, email, StringComparison.OrdinalIgnoreCase))
                            context.Fail("Usuario no válido o inactivo.");
                    }
                };
            })
            .AddJwtBearer("CustomerBearer", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = ReadEnv("CUSTOMER_JWT_ISSUER"),
                    ValidAudience = ReadEnv("CUSTOMER_JWT_AUDIENCE"),
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            ReadEnv("CUSTOMER_JWT_KEY")
                            ?? throw new Exception("CUSTOMER_JWT_KEY no definida")
                        )
                    ),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var subject = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                            ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var email = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Email)
                            ?? context.Principal?.FindFirstValue(ClaimTypes.Email);

                        if (!Guid.TryParse(subject, out var customerId) || string.IsNullOrWhiteSpace(email))
                        {
                            context.Fail("Token de cliente inválido.");
                            return;
                        }

                        var cliente = await db.UsuariosCliente.FirstOrDefaultAsync(u => u.IdUsuarioCliente == customerId);
                        if (cliente == null || !cliente.Activo || !cliente.EmailVerificado || !string.Equals(cliente.Email, email, StringComparison.OrdinalIgnoreCase))
                            context.Fail("Cliente no válido o inactivo.");
                    }
                };
            });

        services.AddAuthorization();
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<LoginService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<RegisterService>();
        services.AddScoped<StaffService>();
        services.AddScoped<CategoriaService>();
        services.AddScoped<IngredienteService>();
        services.AddScoped<PlatoService>();
        services.AddScoped<PedidoService>();
        services.AddScoped<MesaService>();
        services.AddScoped<FacturaService>();
        services.AddScoped<MesaPublicSessionService>();
        services.AddScoped<ICustomerJwtService, CustomerJwtService>();
        services.AddScoped<CustomerAccountService>();
        services.AddScoped<MockPaymentService>();
        services.AddScoped<PublicCheckoutService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddHttpClient<IEmployeeImageService, CloudinaryEmployeeImageService>();
        services.AddControllers();
    })
    .Configure(app =>
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Console.WriteLine("Aplicando migraciones...");
        db.Database.Migrate();
        db.Database.ExecuteSqlRaw("""
            ALTER TABLE "Pedidos"
            ADD COLUMN IF NOT EXISTS "GastosEnvio" numeric(10,2) NOT NULL DEFAULT 0;
            """);
        Console.WriteLine("Migraciones aplicadas.");
        Console.WriteLine("Ejecutando seed por defecto...");
        DbInitializer.SeedDefaultEmployeesAsync(db).GetAwaiter().GetResult();
        DbInitializer.SeedDefaultCustomersAsync(db).GetAwaiter().GetResult();
        DbInitializer.CleanupOrphanFacturasAsync(db).GetAwaiter().GetResult();
        Console.WriteLine("Seed completado.");

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors("LocalPolicy");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    })
    .Build();

Console.WriteLine("Host construido.");
host.Run();
