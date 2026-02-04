using Gestaurante.Models.Data;
using Gestaurante.Models.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    });


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));


// IMPORTANTE!!
// Aquí se añaden los "scoped", todos los servicios que se vayan a usar en la App
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<RegisterService>();
builder.Services.AddScoped<StaffService>();
builder.Services.AddScoped<IngredienteService>();





// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //db.Database.Migrate();
    //DbInitializer.Seed(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();
app.UseCors("LocalPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
