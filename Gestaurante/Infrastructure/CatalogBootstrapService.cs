using System.Text.Json;
using Gestaurante.Models.Data;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Infrastructure
{
    public interface ICatalogBootstrapService
    {
        Task<CatalogImportResult> ImportAsync(string? configuredPath = null, CancellationToken cancellationToken = default);
    }

    public sealed record CatalogImportResult(
        int Categorias,
        int Ingredientes,
        int Platos,
        string SourcePath
    );

    public class CatalogBootstrapService : ICatalogBootstrapService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly AppDbContext _db;
        private readonly ILogger<CatalogBootstrapService> _logger;

        public CatalogBootstrapService(AppDbContext db, ILogger<CatalogBootstrapService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<CatalogImportResult> ImportAsync(string? configuredPath = null, CancellationToken cancellationToken = default)
        {
            var sourcePath = ResolveCatalogPath(configuredPath)
                ?? throw new FileNotFoundException("No se ha encontrado el fichero del catÃ¡logo para importar.");

            await using var stream = File.OpenRead(sourcePath);
            var payload = await JsonSerializer.DeserializeAsync<CatalogImportPayload>(stream, JsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("No se ha podido leer el catÃ¡logo de importaciÃ³n.");

            ValidatePayload(payload);

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            await UpsertCategoriasAsync(payload.Categorias, cancellationToken);
            await UpsertIngredientesAsync(payload.Ingredientes, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            await UpsertPlatosAsync(payload.Platos, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "CatÃ¡logo importado desde {SourcePath}. CategorÃ­as={Categorias}, Ingredientes={Ingredientes}, Platos={Platos}",
                sourcePath,
                payload.Categorias.Count,
                payload.Ingredientes.Count,
                payload.Platos.Count);

            return new CatalogImportResult(
                payload.Categorias.Count,
                payload.Ingredientes.Count,
                payload.Platos.Count,
                sourcePath);
        }

        private async Task UpsertCategoriasAsync(
            IReadOnlyCollection<CatalogCategoriaPayload> categorias,
            CancellationToken cancellationToken)
        {
            var existentes = await _db.Categorias
                .ToDictionaryAsync(categoria => categoria.IdCategoria, cancellationToken);

            foreach (var categoriaPayload in categorias)
            {
                var categoriaId = ParseGuid(categoriaPayload.IdCategoria, nameof(categoriaPayload.IdCategoria));
                var descripcion = (categoriaPayload.Descripcion ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(descripcion))
                    throw new InvalidOperationException($"La categorÃ­a {categoriaId} no tiene descripciÃ³n.");

                if (existentes.TryGetValue(categoriaId, out var categoria))
                {
                    categoria.Descripcion = descripcion;
                    continue;
                }

                var nuevaCategoria = new Categoria(categoriaId, descripcion);
                await _db.Categorias.AddAsync(nuevaCategoria, cancellationToken);
                existentes[categoriaId] = nuevaCategoria;
            }
        }

        private async Task UpsertIngredientesAsync(
            IReadOnlyCollection<CatalogIngredientePayload> ingredientes,
            CancellationToken cancellationToken)
        {
            var existentes = await _db.Ingredientes
                .ToDictionaryAsync(ingrediente => ingrediente.IdIngrediente, cancellationToken);

            foreach (var ingredientePayload in ingredientes)
            {
                var ingredienteId = ParseGuid(ingredientePayload.IdIngrediente, nameof(ingredientePayload.IdIngrediente));
                var nombre = (ingredientePayload.Nombre ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(nombre))
                    throw new InvalidOperationException($"El ingrediente {ingredienteId} no tiene nombre.");

                if (existentes.TryGetValue(ingredienteId, out var ingrediente))
                {
                    ingrediente.Nombre = nombre;
                    ingrediente.Alergenico = ingredientePayload.Alergenico;
                    ingrediente.Disponible = ingredientePayload.Disponible;
                    ingrediente.Imagen = ingredientePayload.Imagen?.Trim() ?? string.Empty;
                    ingrediente.UpdatedAt = DateTime.UtcNow;
                    continue;
                }

                var nuevoIngrediente = new Ingrediente(
                    ingredienteId,
                    nombre,
                    ingredientePayload.Alergenico,
                    ingredientePayload.Disponible,
                    ingredientePayload.Imagen?.Trim() ?? string.Empty);

                await _db.Ingredientes.AddAsync(nuevoIngrediente, cancellationToken);
                existentes[ingredienteId] = nuevoIngrediente;
            }
        }

        private async Task UpsertPlatosAsync(
            IReadOnlyCollection<CatalogPlatoPayload> platos,
            CancellationToken cancellationToken)
        {
            var categoriasExistentes = await _db.Categorias
                .AsNoTracking()
                .Select(categoria => categoria.IdCategoria)
                .ToHashSetAsync(cancellationToken);

            var ingredientesExistentes = await _db.Ingredientes
                .AsNoTracking()
                .Select(ingrediente => ingrediente.IdIngrediente)
                .ToHashSetAsync(cancellationToken);

            var platosExistentes = await _db.Platos
                .Include(plato => plato.PlatoIngredientes)
                .ToListAsync(cancellationToken);

            var platosPorId = platosExistentes.ToDictionary(plato => plato.IdPlato);
            var platosPorNombre = platosExistentes
                .GroupBy(plato => NormalizeLookupKey(plato.Nombre), StringComparer.OrdinalIgnoreCase)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key))
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var platosSincronizados = new HashSet<Guid>();

            foreach (var platoPayload in platos)
            {
                var platoId = ParseGuid(platoPayload.IdPlato, nameof(platoPayload.IdPlato));
                var categoriaId = ParseGuid(platoPayload.IdCategoria, nameof(platoPayload.IdCategoria));
                var nombre = (platoPayload.Nombre ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(nombre))
                    throw new InvalidOperationException($"El plato {platoId} no tiene nombre.");

                if (!categoriasExistentes.Contains(categoriaId))
                    throw new InvalidOperationException($"La categorÃ­a {categoriaId} no existe para el plato {nombre}.");

                var ingredienteIds = (platoPayload.Ingredientes ?? [])
                    .Select(ingrediente => ParseGuid(ingrediente.IdIngrediente, nameof(ingrediente.IdIngrediente)))
                    .Distinct()
                    .ToHashSet();

                var ingredienteFaltante = ingredienteIds.FirstOrDefault(id => !ingredientesExistentes.Contains(id));
                if (ingredienteFaltante != Guid.Empty)
                    throw new InvalidOperationException($"El ingrediente {ingredienteFaltante} no existe para el plato {nombre}.");

                var lookupKey = NormalizeLookupKey(nombre);
                if (!platosPorId.TryGetValue(platoId, out var plato) && !platosPorNombre.TryGetValue(lookupKey, out plato))
                {
                    plato = new Plato(
                        platoId,
                        nombre,
                        ResolveDescripcion(platoPayload),
                        platoPayload.Imagen?.Trim() ?? string.Empty,
                        platoPayload.Disponible,
                        Convert.ToDecimal(platoPayload.Precio),
                        categoriaId);

                    await _db.Platos.AddAsync(plato, cancellationToken);
                    platosPorId[plato.IdPlato] = plato;
                    platosPorNombre[lookupKey] = plato;
                }
                else
                {
                    plato.Nombre = nombre;
                    plato.Descripcion = ResolveDescripcion(platoPayload);
                    plato.Imagen = platoPayload.Imagen?.Trim() ?? string.Empty;
                    plato.Disponible = platoPayload.Disponible;
                    plato.Precio = Convert.ToDecimal(platoPayload.Precio);
                    plato.IdCategoria = categoriaId;
                    plato.UpdatedAt = DateTime.UtcNow;
                }

                SyncIngredientes(plato, ingredienteIds);
                platosSincronizados.Add(plato.IdPlato);
            }

            foreach (var platoExistente in platosExistentes.Where(plato => !platosSincronizados.Contains(plato.IdPlato) && plato.Disponible))
            {
                platoExistente.Disponible = false;
                platoExistente.UpdatedAt = DateTime.UtcNow;
            }
        }

        private void SyncIngredientes(Plato plato, HashSet<Guid> ingredientIds)
        {
            var actuales = plato.PlatoIngredientes
                .Select(platoIngrediente => platoIngrediente.IdIngrediente)
                .ToHashSet();

            var aEliminar = plato.PlatoIngredientes
                .Where(platoIngrediente => !ingredientIds.Contains(platoIngrediente.IdIngrediente))
                .ToList();

            if (aEliminar.Count > 0)
            {
                _db.RemoveRange(aEliminar);
                foreach (var platoIngrediente in aEliminar)
                    plato.PlatoIngredientes.Remove(platoIngrediente);
            }

            foreach (var ingredienteId in ingredientIds.Except(actuales))
                plato.PlatoIngredientes.Add(new PlatoIngrediente(plato.IdPlato, ingredienteId));
        }

        private static string ResolveDescripcion(CatalogPlatoPayload platoPayload)
        {
            var descripcion = (platoPayload.Descripcion ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(descripcion))
                return descripcion;

            if (platoPayload.IngredientesTexto is { Count: > 0 })
                return string.Join(", ", platoPayload.IngredientesTexto.Where(texto => !string.IsNullOrWhiteSpace(texto)));

            return "Plato del catÃ¡logo importado.";
        }

        private static void ValidatePayload(CatalogImportPayload payload)
        {
            if (payload.Categorias.Count == 0)
                throw new InvalidOperationException("El catÃ¡logo de importaciÃ³n no contiene categorÃ­as.");

            if (payload.Ingredientes.Count == 0)
                throw new InvalidOperationException("El catÃ¡logo de importaciÃ³n no contiene ingredientes.");

            if (payload.Platos.Count == 0)
                throw new InvalidOperationException("El catÃ¡logo de importaciÃ³n no contiene platos.");
        }

        private static Guid ParseGuid(string? rawValue, string fieldName)
        {
            if (Guid.TryParse(rawValue, out var value))
                return value;

            throw new InvalidOperationException($"El valor '{rawValue}' no es un GUID vÃ¡lido para {fieldName}.");
        }

        private static string NormalizeLookupKey(string value)
        {
            return string.Join(
                ' ',
                (value ?? string.Empty)
                    .Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();
        }

        private static string? ResolveCatalogPath(string? configuredPath)
        {
            var candidatePaths = new List<string>();
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                candidatePaths.Add(Path.IsPathRooted(configuredPath)
                    ? configuredPath
                    : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredPath)));
            }

            candidatePaths.AddRange(
                [
                    Path.Combine(Directory.GetCurrentDirectory(), "depizzeo-menu-import-ready.json"),
                    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "depizzeo-menu-import-ready.json")),
                    Path.Combine(AppContext.BaseDirectory, "depizzeo-menu-import-ready.json"),
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "depizzeo-menu-import-ready.json")),
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "depizzeo-menu-import-ready.json"))
                ]);

            return candidatePaths
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(File.Exists);
        }

        private sealed class CatalogImportPayload
        {
            public List<CatalogCategoriaPayload> Categorias { get; set; } = [];
            public List<CatalogIngredientePayload> Ingredientes { get; set; } = [];
            public List<CatalogPlatoPayload> Platos { get; set; } = [];
        }

        private sealed class CatalogCategoriaPayload
        {
            public string? IdCategoria { get; set; }
            public string? Descripcion { get; set; }
        }

        private sealed class CatalogIngredientePayload
        {
            public string? IdIngrediente { get; set; }
            public string? Nombre { get; set; }
            public bool Alergenico { get; set; }
            public bool Disponible { get; set; } = true;
            public string? Imagen { get; set; }
        }

        private sealed class CatalogPlatoPayload
        {
            public string? IdPlato { get; set; }
            public string? Nombre { get; set; }
            public string? Descripcion { get; set; }
            public List<string> IngredientesTexto { get; set; } = [];
            public string? Imagen { get; set; }
            public bool Disponible { get; set; } = true;
            public double Precio { get; set; }
            public string? IdCategoria { get; set; }
            public List<CatalogPlatoIngredientePayload> Ingredientes { get; set; } = [];
        }

        private sealed class CatalogPlatoIngredientePayload
        {
            public string? IdIngrediente { get; set; }
        }
    }
}
