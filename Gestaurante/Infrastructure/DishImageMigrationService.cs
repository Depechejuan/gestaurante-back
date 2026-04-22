using System.Text.Json;
using Gestaurante.Configuration;
using Gestaurante.Models.Data;
using Gestaurante.Models.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gestaurante.Infrastructure
{
    public interface IDishImageMigrationService
    {
        /// <summary>
        /// Migra las imágenes externas de platos a Cloudinary y deja un reporte JSON con el resultado.
        /// </summary>
        /// <param name="configuredReportPath">Ruta opcional del reporte. Si no se indica, se resuelve una ruta por defecto bajo <c>_runtime_logs</c>.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Reporte completo de la ejecución, incluyendo resumen y detalle por plato.</returns>
        Task<DishImageMigrationReport> RunAsync(string? configuredReportPath = null, CancellationToken cancellationToken = default);
    }

    public sealed record DishImageMigrationReport(
        DateTime StartedAtUtc,
        DateTime FinishedAtUtc,
        int Scanned,
        int Candidates,
        int Migrated,
        int Skipped,
        int Failed,
        string ReportPath,
        IReadOnlyList<DishImageMigrationItem> Items);

    public sealed record DishImageMigrationItem(
        Guid DishId,
        string DishName,
        string OriginalUrl,
        string Status,
        string? FinalUrl = null,
        string? Error = null);

    public class DishImageMigrationService : IDishImageMigrationService
    {
        private static readonly JsonSerializerOptions ReportJsonOptions = new()
        {
            WriteIndented = true
        };

        private readonly AppDbContext _db;
        private readonly IPlatoImageService _platoImageService;
        private readonly CloudinaryOptions _cloudinaryOptions;
        private readonly ILogger<DishImageMigrationService> _logger;

        public DishImageMigrationService(
            AppDbContext db,
            IPlatoImageService platoImageService,
            IOptions<CloudinaryOptions> cloudinaryOptions,
            ILogger<DishImageMigrationService> logger)
        {
            _db = db;
            _platoImageService = platoImageService;
            _cloudinaryOptions = cloudinaryOptions.Value;
            _logger = logger;
        }

        /// <summary>
        /// Migra las imágenes externas de platos a Cloudinary y deja un reporte JSON con el resultado.
        /// </summary>
        /// <param name="configuredReportPath">Ruta opcional del reporte. Si no se indica, se resuelve una ruta por defecto bajo <c>_runtime_logs</c>.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Reporte completo de la ejecución, incluyendo resumen y detalle por plato.</returns>
        public async Task<DishImageMigrationReport> RunAsync(string? configuredReportPath = null, CancellationToken cancellationToken = default)
        {
            var startedAtUtc = DateTime.UtcNow;
            var items = new List<DishImageMigrationItem>();
            var scanned = 0;
            var candidates = 0;
            var migrated = 0;
            var skipped = 0;
            var failed = 0;

            var platos = await _db.Platos
                .Where(plato => !string.IsNullOrWhiteSpace(plato.Imagen))
                .OrderBy(plato => plato.Nombre)
                .ToListAsync(cancellationToken);

            scanned = platos.Count;

            foreach (var plato in platos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var originalUrl = plato.Imagen?.Trim() ?? string.Empty;
                if (_cloudinaryOptions.IsCloudinaryUrl(originalUrl))
                {
                    skipped++;
                    items.Add(new DishImageMigrationItem(
                        plato.IdPlato,
                        plato.Nombre,
                        originalUrl,
                        "skipped",
                        FinalUrl: originalUrl,
                        Error: "La imagen ya apunta a Cloudinary."));
                    continue;
                }

                if (!_cloudinaryOptions.IsRemoteHttpUrl(originalUrl))
                {
                    skipped++;
                    items.Add(new DishImageMigrationItem(
                        plato.IdPlato,
                        plato.Nombre,
                        originalUrl,
                        "skipped",
                        FinalUrl: originalUrl,
                        Error: "La imagen no es una URL HTTP/HTTPS pública."));
                    continue;
                }

                candidates++;
                var originalUpdatedAt = plato.UpdatedAt;

                try
                {
                    var finalUrl = await _platoImageService.UploadOrReplaceDishImageFromUrlAsync(plato.IdPlato, originalUrl, cancellationToken);
                    plato.Imagen = finalUrl;
                    plato.UpdatedAt = DateTime.UtcNow;

                    await _db.SaveChangesAsync(cancellationToken);

                    migrated++;
                    items.Add(new DishImageMigrationItem(
                        plato.IdPlato,
                        plato.Nombre,
                        originalUrl,
                        "migrated",
                        FinalUrl: finalUrl));
                }
                catch (Exception ex)
                {
                    plato.Imagen = originalUrl;
                    plato.UpdatedAt = originalUpdatedAt;
                    _db.Entry(plato).State = EntityState.Unchanged;

                    failed++;
                    items.Add(new DishImageMigrationItem(
                        plato.IdPlato,
                        plato.Nombre,
                        originalUrl,
                        "failed",
                        Error: ex.Message));

                    _logger.LogError(ex, "No se ha podido migrar la imagen del plato {DishId} ({DishName}).", plato.IdPlato, plato.Nombre);
                }
            }

            var finishedAtUtc = DateTime.UtcNow;
            var reportPath = ResolveReportPath(configuredReportPath, finishedAtUtc);
            var report = new DishImageMigrationReport(
                startedAtUtc,
                finishedAtUtc,
                scanned,
                candidates,
                migrated,
                skipped,
                failed,
                reportPath,
                items);

            await WriteReportAsync(report, cancellationToken);

            _logger.LogInformation(
                "Migración de imágenes de platos finalizada. Escaneados={Scanned} Candidatos={Candidates} Migrados={Migrated} Omitidos={Skipped} Fallidos={Failed} Reporte={ReportPath}",
                scanned,
                candidates,
                migrated,
                skipped,
                failed,
                reportPath);

            return report;
        }

        private static string ResolveReportPath(string? configuredReportPath, DateTime finishedAtUtc)
        {
            if (!string.IsNullOrWhiteSpace(configuredReportPath))
            {
                return Path.IsPathRooted(configuredReportPath)
                    ? configuredReportPath
                    : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredReportPath));
            }

            var fileName = $"plato-image-migration-{finishedAtUtc:yyyyMMddTHHmmssZ}.json";
            var candidateDirectories = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "_runtime_logs"),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "_runtime_logs")),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "_runtime_logs")),
                Path.Combine(AppContext.BaseDirectory, "_runtime_logs"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "_runtime_logs")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "_runtime_logs")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "_runtime_logs")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "_runtime_logs"))
            };

            var selectedDirectory = candidateDirectories
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(Directory.Exists)
                ?? candidateDirectories.First();

            return Path.Combine(selectedDirectory, fileName);
        }

        private static async Task WriteReportAsync(DishImageMigrationReport report, CancellationToken cancellationToken)
        {
            var directory = Path.GetDirectoryName(report.ReportPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await using var stream = File.Create(report.ReportPath);
            await JsonSerializer.SerializeAsync(stream, report, ReportJsonOptions, cancellationToken);
        }
    }
}
