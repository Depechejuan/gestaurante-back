using Gestaurante.Models.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
                ["CLOUDINARY_CLOUDNAME"] = FakePlatoImageService.CloudName
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.RemoveAll<IEmployeeImageService>();
            services.RemoveAll<IPlatoImageService>();

            services.AddSingleton<FakeEmailService>();
            services.AddSingleton<FakePlatoImageService>();
            services.AddSingleton<IEmailService>(provider => provider.GetRequiredService<FakeEmailService>());
            services.AddSingleton<IEmployeeImageService, FakeEmployeeImageService>();
            services.AddSingleton<IPlatoImageService>(provider => provider.GetRequiredService<FakePlatoImageService>());
        });
    }
}
