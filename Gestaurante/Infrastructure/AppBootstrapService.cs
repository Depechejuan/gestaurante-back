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
        private readonly SeedOptions _seedOptions;
        private readonly BootstrapOptions _bootstrapOptions;
        private readonly ILogger<AppBootstrapService> _logger;

        public AppBootstrapService(
            AppDbContext db,
            IOptions<SeedOptions> seedOptions,
            IOptions<BootstrapOptions> bootstrapOptions,
            ILogger<AppBootstrapService> logger)
        {
            _db = db;
            _seedOptions = seedOptions.Value;
            _bootstrapOptions = bootstrapOptions.Value;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Bootstrap explícito activado. ApplyMigrations={ApplyMigrations} SeedDefaults={SeedDefaults} RepairData={RepairData}",
                _bootstrapOptions.ApplyMigrations,
                _bootstrapOptions.SeedDefaults,
                _bootstrapOptions.RepairData);

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
        }
    }
}
