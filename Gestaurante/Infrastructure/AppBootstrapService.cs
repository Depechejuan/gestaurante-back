using Gestaurante.Configuration;
using Gestaurante.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gestaurante.Infrastructure
{
    public interface IAppBootstrapService
    {
        Task RunAsync(CancellationToken cancellationToken = default);
    }

    public class AppBootstrapService : IAppBootstrapService
    {
        private readonly AppDbContext _db;
        private readonly ICatalogBootstrapService _catalogBootstrapService;
        private readonly IDishImageMigrationService _dishImageMigrationService;
        private readonly SeedOptions _seedOptions;
        private readonly BootstrapOptions _bootstrapOptions;
        private readonly ILogger<AppBootstrapService> _logger;

        public AppBootstrapService(
            AppDbContext db,
            ICatalogBootstrapService catalogBootstrapService,
            IDishImageMigrationService dishImageMigrationService,
            IOptions<SeedOptions> seedOptions,
            IOptions<BootstrapOptions> bootstrapOptions,
            ILogger<AppBootstrapService> logger)
        {
            _db = db;
            _catalogBootstrapService = catalogBootstrapService;
            _dishImageMigrationService = dishImageMigrationService;
            _seedOptions = seedOptions.Value;
            _bootstrapOptions = bootstrapOptions.Value;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Bootstrap explícito activado. ApplyMigrations={ApplyMigrations} SeedDefaults={SeedDefaults} RepairData={RepairData} ImportCatalog={ImportCatalog} MigrateDishImages={MigrateDishImages}",
                _bootstrapOptions.ApplyMigrations,
                _bootstrapOptions.SeedDefaults,
                _bootstrapOptions.RepairData,
                _bootstrapOptions.ImportCatalog,
                _bootstrapOptions.MigrateDishImages);

            if (_bootstrapOptions.ApplyMigrations)
            {
                _logger.LogInformation("Aplicando migraciones...");
                await _db.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Migraciones aplicadas.");
            }

            if (_bootstrapOptions.RepairData)
            {
                _logger.LogInformation("Aplicando reparaciones de datos...");
                await _db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE "Pedidos"
                    ADD COLUMN IF NOT EXISTS "GastosEnvio" numeric(10,2) NOT NULL DEFAULT 0;
                    """, cancellationToken);
                await _db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE "Ingredientes"
                    ALTER COLUMN "Nombre" TYPE character varying(255);
                    """, cancellationToken);
                await DbInitializer.CleanupOrphanFacturasAsync(_db, cancellationToken);
                _logger.LogInformation("Reparaciones completadas.");
            }

            if (_bootstrapOptions.SeedDefaults)
            {
                _seedOptions.EnsureReady();
                _logger.LogInformation("Ejecutando seed por defecto...");
                await DbInitializer.SeedDefaultEmployeesAsync(_db, cancellationToken);
                await DbInitializer.SeedDefaultCustomersAsync(_db, cancellationToken);
                _logger.LogInformation("Seed completado.");
            }

            if (_bootstrapOptions.ImportCatalog)
            {
                _logger.LogInformation("Importando catálogo desde {CatalogPath}...", _bootstrapOptions.CatalogImportPath ?? "ruta automática");
                var result = await _catalogBootstrapService.ImportAsync(_bootstrapOptions.CatalogImportPath, cancellationToken);
                _logger.LogInformation(
                    "Catálogo importado correctamente. Categorías={Categorias} Ingredientes={Ingredientes} Platos={Platos}",
                    result.Categorias,
                    result.Ingredientes,
                    result.Platos);
            }

            if (_bootstrapOptions.MigrateDishImages)
            {
                _logger.LogInformation("Migrando imágenes de platos a Cloudinary...");
                var report = await _dishImageMigrationService.RunAsync(_bootstrapOptions.DishImageReportPath, cancellationToken);
                _logger.LogInformation(
                    "Migración de imágenes completada. Migrados={Migrated} Omitidos={Skipped} Fallidos={Failed} Reporte={ReportPath}",
                    report.Migrated,
                    report.Skipped,
                    report.Failed,
                    report.ReportPath);
            }
        }
    }
}
