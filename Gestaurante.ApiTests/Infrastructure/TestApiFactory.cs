using Gestaurante.Configuration;
using Gestaurante.Models.Data;
using Gestaurante.Models.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.ApiTests.Infrastructure;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    public const string TestDatabaseName = "requier_test";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DB_NAME"] = TestDatabaseName,
                ["PORT"] = "0",
                ["BOOTSTRAP_ON_START"] = "false",
                ["CORS_ALLOWED_ORIGINS"] = "http://localhost:4173",
                ["CONTACT_TO_EMAIL"] = "contact@gestaurante.local",
                ["CLOUDINARY_CLOUDNAME"] = FakePlatoImageService.CloudName
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IEmailService>();
            services.RemoveAll<IEmployeeImageService>();
            services.RemoveAll<IPlatoImageService>();

            var databaseOptions = new DatabaseOptions
            {
                Host = GetRequiredEnv("DB_HOST"),
                Port = int.TryParse(Environment.GetEnvironmentVariable("DB_PORT"), out var parsedPort) ? parsedPort : 5432,
                Name = TestDatabaseName,
                User = GetRequiredEnv("DB_USER"),
                Password = GetRequiredEnv("DB_PASSWORD"),
                RequireSsl = !string.Equals(Environment.GetEnvironmentVariable("PGSSLMODE"), "disable", StringComparison.OrdinalIgnoreCase)
            };

            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(databaseOptions.BuildConnectionString()));

            services.AddSingleton<FakeEmailService>();
            services.AddSingleton<FakePlatoImageService>();
            services.AddSingleton<IEmailService>(provider => provider.GetRequiredService<FakeEmailService>());
            services.AddSingleton<IEmployeeImageService, FakeEmployeeImageService>();
            services.AddSingleton<IPlatoImageService>(provider => provider.GetRequiredService<FakePlatoImageService>());
        });
    }

    private static string GetRequiredEnv(string key)
    {
        return Environment.GetEnvironmentVariable(key)?.Trim('\'', '"')
            ?? throw new InvalidOperationException($"{key} no definido para pruebas.");
    }
}
