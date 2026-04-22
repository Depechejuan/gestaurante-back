using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Gestaurante.Configuration;
using Gestaurante.Infrastructure;
using Gestaurante.Middleware;
using Gestaurante.Models.Data;
using Gestaurante.Models.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

AppConfiguration.LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);
builder.Services.RegisterApplicationOptions(builder.Configuration, args);

var databaseOptions = builder.Configuration.BuildDatabaseOptions();
var employeeJwtOptions = builder.Configuration.BuildEmployeeJwtOptions();
var customerJwtOptions = builder.Configuration.BuildCustomerJwtOptions();
var bootstrapOptions = builder.Configuration.BuildBootstrapOptions(args);
var corsPolicyOptions = builder.Configuration.BuildCorsPolicyOptions();
var allowedOrigins = corsPolicyOptions.AllowedOrigins
    .Select(NormalizeOrigin)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
var appPort = builder.Configuration.GetTrimmedValue("PORT") ?? "3000";

builder.WebHost.UseUrls($"http://localhost:{appPort}");

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            allowedOrigins.Count == 0
            || allowedOrigins.Contains(NormalizeOrigin(origin))
            || IsLoopbackOrigin(origin))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = employeeJwtOptions.Issuer,
            ValidAudience = employeeJwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(employeeJwtOptions.Key)),
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
            ValidIssuer = customerJwtOptions.Issuer,
            ValidAudience = customerJwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(customerJwtOptions.Key)),
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

builder.Services.AddAuthorization();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(databaseOptions.BuildConnectionString()));
builder.Services.AddScoped<IAppBootstrapService, AppBootstrapService>();
builder.Services.AddScoped<ICatalogBootstrapService, CatalogBootstrapService>();
builder.Services.AddScoped<IDishImageMigrationService, DishImageMigrationService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<RegisterService>();
builder.Services.AddScoped<StaffService>();
builder.Services.AddScoped<CategoriaService>();
builder.Services.AddScoped<IngredienteService>();
builder.Services.AddScoped<CatalogProjectionService>();
builder.Services.AddScoped<PlatoService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<MesaService>();
builder.Services.AddScoped<FacturaService>();
builder.Services.AddScoped<MesaPublicSessionService>();
builder.Services.AddScoped<ICustomerJwtService, CustomerJwtService>();
builder.Services.AddScoped<CustomerAccountService>();
builder.Services.AddScoped<SimulatedPaymentService>();
builder.Services.AddScoped<PublicCheckoutService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddScoped<IEmployeeImageService, CloudinaryEmployeeImageService>();
builder.Services.AddScoped<IPlatoImageService, CloudinaryPlatoImageService>();
builder.Services.AddHealthChecks();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Gestaurante.Startup");

logger.LogInformation("Gestaurante API iniciando en localhost:{Port}", appPort);

if (bootstrapOptions.RunOnStart)
{
    using var bootstrapScope = app.Services.CreateScope();
    var bootstrapper = bootstrapScope.ServiceProvider.GetRequiredService<IAppBootstrapService>();
    await bootstrapper.RunAsync();
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", async (AppDbContext db, IWebHostEnvironment environment, CancellationToken cancellationToken) =>
{
    var databaseReachable = await db.Database.CanConnectAsync(cancellationToken);
    var statusCode = databaseReachable ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;

    return Results.Json(new
    {
        status = databaseReachable ? "ok" : "degraded",
        environment = environment.EnvironmentName,
        database = databaseReachable ? "reachable" : "unreachable",
        bootstrapEnabled = bootstrapOptions.RunOnStart
    }, statusCode: statusCode);
});

app.Run();

static string NormalizeOrigin(string origin)
{
    return string.IsNullOrWhiteSpace(origin)
        ? string.Empty
        : origin.Trim().TrimEnd('/');
}

static bool IsLoopbackOrigin(string origin)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        return false;

    if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        return false;

    var host = uri.Host.Trim();
    if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        return true;

    return System.Net.IPAddress.TryParse(host, out var ipAddress)
        && System.Net.IPAddress.IsLoopback(ipAddress);
}

public partial class Program;
