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
//using Gestaurante.Models.Seed;


// Cargar variables del .env
Env.Load();

// Creación de la Connection String
string dbHost = Environment.GetEnvironmentVariable("DB_HOST")
    ?? throw new Exception("DB_HOST no definido");

string dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
string dbName = Environment.GetEnvironmentVariable("DB_NAME")
    ?? throw new Exception("DB_NAME no definido");

string dbUser = Environment.GetEnvironmentVariable("DB_USER")
    ?? throw new Exception("DB_USER no definido");

string dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD")
    ?? throw new Exception("DB_PASSWORD no definido");


string connectionString =
    $"Server={dbHost};Port={dbPort};Database={dbName};User Id={dbUser};Password={dbPassword};SSL Mode=Require;Trust Server Certificate=true;";


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "https://localhost:5173"
            )
            .AllowAnyHeader()
            .WithMethods("PUT", "PATCH", "POST", "GET", "DELETE");
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

            ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
            ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    Environment.GetEnvironmentVariable("JWT_KEY")
                    ?? throw new Exception("JWT_KEY no definida")
                )
            ),

            ClockSkew = TimeSpan.Zero // elimina tolerancia de 5 min por defecto
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
                {
                    context.Fail("Usuario no válido o inactivo.");
                }
            }
        };
    });


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));


// IMPORTANTE!!
// Aquí se añaden los "scoped", todos los servicios que se vayan a usar en la App
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<RegisterService>();
builder.Services.AddScoped<StaffService>();
<<<<<<< HEAD
builder.Services.AddScoped<CloudinaryService>();
=======
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<MesaService>();
builder.Services.AddScoped<FacturaService>();
builder.Services.AddHttpClient<IEmployeeImageService, CloudinaryEmployeeImageService>();
>>>>>>> faae158368c361197c0256daa19653d3690ac784





// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    //DbInitializer.Seed(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("LocalPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
