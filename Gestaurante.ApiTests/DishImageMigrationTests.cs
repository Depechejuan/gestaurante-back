using System.Text.Json;
using Gestaurante.ApiTests.Infrastructure;
using Gestaurante.Infrastructure;
using Gestaurante.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gestaurante.ApiTests;

[Collection(ApiCollection.Name)]
public sealed class DishImageMigrationTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task DishImageMigrationMigratesExternalUrlsWritesReportAndLeavesFailuresUntouched()
    {
        using var scope = Fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var migrationService = scope.ServiceProvider.GetRequiredService<IDishImageMigrationService>();
        var validUrl = "https://cdn.example.com/platos/caprese.jpg";
        var brokenUrl = "https://cdn.example.com/platos/pizza-broken.jpg";
        var reportPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-dish-report.json");

        var caprese = await db.Platos.FirstAsync(plato => plato.IdPlato == Fixture.State.PlatoCapreseId);
        var pizza = await db.Platos.FirstAsync(plato => plato.IdPlato == Fixture.State.PlatoPizzaId);
        caprese.Imagen = validUrl;
        pizza.Imagen = brokenUrl;
        await db.SaveChangesAsync();

        Fixture.PlatoImageService.FailRemoteUrl(brokenUrl);

        var report = await migrationService.RunAsync(reportPath);

        report.Scanned.Should().Be(2);
        report.Candidates.Should().Be(2);
        report.Migrated.Should().Be(1);
        report.Failed.Should().Be(1);
        report.ReportPath.Should().Be(reportPath);
        report.Items.Should().Contain(item =>
            item.DishId == Fixture.State.PlatoCapreseId
            && item.Status == "migrated"
            && item.FinalUrl != null);
        report.Items.Should().Contain(item =>
            item.DishId == Fixture.State.PlatoPizzaId
            && item.Status == "failed"
            && item.Error != null);

        await db.Entry(caprese).ReloadAsync();
        await db.Entry(pizza).ReloadAsync();

        caprese.Imagen.Should().StartWith("https://res.cloudinary.com/test-cloud/");
        pizza.Imagen.Should().Be(brokenUrl);

        File.Exists(reportPath).Should().BeTrue();
        using var reportDocument = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
        reportDocument.RootElement.GetProperty("Migrated").GetInt32().Should().Be(1);
        reportDocument.RootElement.GetProperty("Failed").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task DishImageMigrationIsIdempotentForAlreadyMigratedDishes()
    {
        using var scope = Fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var migrationService = scope.ServiceProvider.GetRequiredService<IDishImageMigrationService>();
        var externalUrl = "https://cdn.example.com/platos/caprese-idempotent.jpg";

        var caprese = await db.Platos.FirstAsync(plato => plato.IdPlato == Fixture.State.PlatoCapreseId);
        caprese.Imagen = externalUrl;
        await db.SaveChangesAsync();

        var firstReportPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-dish-report-first.json");
        var secondReportPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-dish-report-second.json");

        var firstReport = await migrationService.RunAsync(firstReportPath);
        var firstFinalUrl = firstReport.Items
            .Single(item => item.DishId == Fixture.State.PlatoCapreseId && item.Status == "migrated")
            .FinalUrl;

        var secondReport = await migrationService.RunAsync(secondReportPath);

        Fixture.PlatoImageService.RemoteUploadCalls.Should().HaveCount(1);
        secondReport.Migrated.Should().Be(0);
        secondReport.Skipped.Should().BeGreaterThanOrEqualTo(1);
        secondReport.Items.Should().Contain(item =>
            item.DishId == Fixture.State.PlatoCapreseId
            && item.Status == "skipped"
            && item.FinalUrl == firstFinalUrl);

        await db.Entry(caprese).ReloadAsync();
        caprese.Imagen.Should().Be(firstFinalUrl);
    }

    [Fact]
    public async Task CatalogImportUploadsExternalDishImagesOnlyOnceAndKeepsCloudinaryUrlOnReimport()
    {
        using var scope = Fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var catalogBootstrapService = scope.ServiceProvider.GetRequiredService<ICatalogBootstrapService>();
        var externalUrl = "https://cdn.example.com/platos/caprese-import.jpg";
        var importPath = await CreateCatalogImportPayloadAsync(externalUrl);

        var caprese = await db.Platos.FirstAsync(plato => plato.IdPlato == Fixture.State.PlatoCapreseId);
        caprese.Imagen = string.Empty;
        await db.SaveChangesAsync();

        await catalogBootstrapService.ImportAsync(importPath);
        await db.Entry(caprese).ReloadAsync();
        var migratedUrl = caprese.Imagen;

        migratedUrl.Should().StartWith("https://res.cloudinary.com/test-cloud/");
        Fixture.PlatoImageService.RemoteUploadCalls.Should().HaveCount(1);

        await catalogBootstrapService.ImportAsync(importPath);
        await db.Entry(caprese).ReloadAsync();

        caprese.Imagen.Should().Be(migratedUrl);
        Fixture.PlatoImageService.RemoteUploadCalls.Should().HaveCount(1);
    }

    private async Task<string> CreateCatalogImportPayloadAsync(string dishImageUrl)
    {
        var payload = new
        {
            Categorias = new[]
            {
                new
                {
                    IdCategoria = Fixture.State.CategoriaId,
                    Descripcion = "Entrantes"
                }
            },
            Ingredientes = new[]
            {
                new
                {
                    IdIngrediente = Fixture.State.IngredienteTomateId,
                    Nombre = "Tomate",
                    Alergenico = false,
                    Disponible = true,
                    Imagen = string.Empty
                },
                new
                {
                    IdIngrediente = Fixture.State.IngredienteMozzarellaId,
                    Nombre = "Mozzarella",
                    Alergenico = true,
                    Disponible = true,
                    Imagen = string.Empty
                }
            },
            Platos = new[]
            {
                new
                {
                    IdPlato = Fixture.State.PlatoCapreseId,
                    Nombre = "Ensalada Caprese",
                    Descripcion = "Tomate, mozzarella y albahaca.",
                    IngredientesTexto = new[] { "Tomate", "Mozzarella" },
                    Imagen = dishImageUrl,
                    Disponible = true,
                    Precio = 9.50,
                    IdCategoria = Fixture.State.CategoriaId,
                    Ingredientes = new[]
                    {
                        new { IdIngrediente = Fixture.State.IngredienteTomateId },
                        new { IdIngrediente = Fixture.State.IngredienteMozzarellaId }
                    }
                }
            }
        };

        var importPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-catalog-import.json");
        await File.WriteAllTextAsync(importPath, JsonSerializer.Serialize(payload));
        return importPath;
    }
}
