using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class PlatoService
    {
        private readonly AppDbContext _db;

        public PlatoService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<PlatoDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var platos = await _db.Platos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.PlatoIngredientes)
                    .ThenInclude(pi => pi.Ingrediente)
                .OrderBy(p => p.Nombre)
                .ToListAsync(cancellationToken);

            return platos.Select(MapPlato).ToList();
        }

        public async Task<PlatoDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var plato = await _db.Platos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.PlatoIngredientes)
                    .ThenInclude(pi => pi.Ingrediente)
                .FirstOrDefaultAsync(p => p.IdPlato == id, cancellationToken);

            return plato == null ? null : MapPlato(plato);
        }

        public async Task<PlatoDTO> CreateAsync(PlatoDTO dto, CancellationToken cancellationToken = default)
        {
            ValidatePlatoInput(dto);

            var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.IdCategoria == dto.IdCategoria, cancellationToken);
            if (categoria == null)
                throw new InvalidOperationException("La categoria indicada no existe.");

            var duplicate = await _db.Platos.AnyAsync(p => p.Nombre.ToLower() == dto.Nombre.Trim().ToLower(), cancellationToken);
            if (duplicate)
                throw new InvalidOperationException("Ya existe un plato con ese nombre.");

            var ingredienteIds = NormalizeIngredienteIds(dto.Ingredientes);
            var ingredientes = await LoadIngredientesAsync(ingredienteIds, cancellationToken);

            var plato = new Plato(
                dto.IdPlato == Guid.Empty ? Guid.NewGuid() : dto.IdPlato,
                dto.Nombre.Trim(),
                dto.Descripcion.Trim(),
                dto.Imagen?.Trim() ?? string.Empty,
                dto.Disponible,
                dto.Precio,
                dto.IdCategoria
            );

            foreach (var ingrediente in ingredientes)
                plato.PlatoIngredientes.Add(new PlatoIngrediente(plato.IdPlato, ingrediente.IdIngrediente));

            await _db.Platos.AddAsync(plato, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetByIdAsync(plato.IdPlato, cancellationToken))!;
        }

        public async Task<PlatoDTO?> UpdateAsync(Guid id, PlatoDTO dto, CancellationToken cancellationToken = default)
        {
            var plato = await _db.Platos
                .Include(p => p.PlatoIngredientes)
                .FirstOrDefaultAsync(p => p.IdPlato == id, cancellationToken);

            if (plato == null)
                return null;

            ValidatePlatoInput(dto);

            var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.IdCategoria == dto.IdCategoria, cancellationToken);
            if (categoria == null)
                throw new InvalidOperationException("La categoria indicada no existe.");

            var duplicate = await _db.Platos.AnyAsync(
                p => p.IdPlato != id && p.Nombre.ToLower() == dto.Nombre.Trim().ToLower(),
                cancellationToken);
            if (duplicate)
                throw new InvalidOperationException("Ya existe otro plato con ese nombre.");

            var ingredienteIds = NormalizeIngredienteIds(dto.Ingredientes);
            var ingredientes = await LoadIngredientesAsync(ingredienteIds, cancellationToken);

            plato.Nombre = dto.Nombre.Trim();
            plato.Descripcion = dto.Descripcion.Trim();
            plato.Imagen = dto.Imagen?.Trim() ?? string.Empty;
            plato.Disponible = dto.Disponible;
            plato.Precio = dto.Precio;
            plato.IdCategoria = dto.IdCategoria;
            plato.UpdatedAt = DateTime.UtcNow;

            _db.RemoveRange(plato.PlatoIngredientes);
            plato.PlatoIngredientes.Clear();
            foreach (var ingrediente in ingredientes)
                plato.PlatoIngredientes.Add(new PlatoIngrediente(plato.IdPlato, ingrediente.IdIngrediente));

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(plato.IdPlato, cancellationToken))!;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var plato = await _db.Platos
                .Include(p => p.PlatoIngredientes)
                .FirstOrDefaultAsync(p => p.IdPlato == id, cancellationToken);

            if (plato == null)
                return false;

            var usedInPedidos = await _db.DetallesPedido.AnyAsync(d => d.IdPlato == id, cancellationToken);
            if (usedInPedidos)
                throw new InvalidOperationException("No puedes borrar un plato que ya aparece en pedidos.");

            _db.RemoveRange(plato.PlatoIngredientes);
            _db.Platos.Remove(plato);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<List<Ingrediente>> LoadIngredientesAsync(List<Guid> ingredienteIds, CancellationToken cancellationToken)
        {
            if (ingredienteIds.Count == 0)
                return new List<Ingrediente>();

            var ingredientes = await _db.Ingredientes
                .Where(i => ingredienteIds.Contains(i.IdIngrediente))
                .ToListAsync(cancellationToken);

            if (ingredientes.Count != ingredienteIds.Count)
                throw new InvalidOperationException("Uno o varios ingredientes indicados no existen.");

            return ingredientes;
        }

        private static List<Guid> NormalizeIngredienteIds(List<PlatoIngredienteDTO>? ingredientes)
        {
            return (ingredientes ?? new List<PlatoIngredienteDTO>())
                .Select(i => i.IdIngrediente)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
        }

        private static void ValidatePlatoInput(PlatoDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new InvalidOperationException("El nombre del plato es obligatorio.");

            if (string.IsNullOrWhiteSpace(dto.Descripcion))
                throw new InvalidOperationException("La descripcion del plato es obligatoria.");

            if (dto.IdCategoria == Guid.Empty)
                throw new InvalidOperationException("Debes indicar una categoria valida.");

            if (dto.Precio < 0)
                throw new InvalidOperationException("El precio del plato no puede ser negativo.");
        }

        private static PlatoDTO MapPlato(Plato plato)
        {
            return new PlatoDTO
            {
                IdPlato = plato.IdPlato,
                Nombre = plato.Nombre,
                Descripcion = plato.Descripcion,
                Imagen = plato.Imagen,
                Disponible = plato.Disponible,
                Precio = plato.Precio,
                IdCategoria = plato.IdCategoria,
                CategoriaDescripcion = plato.Categoria?.Descripcion ?? string.Empty,
                Ingredientes = plato.PlatoIngredientes
                    .Select(pi => new PlatoIngredienteDTO
                    {
                        IdIngrediente = pi.IdIngrediente,
                        Nombre = pi.Ingrediente?.Nombre ?? string.Empty
                    })
                    .OrderBy(i => i.Nombre)
                    .ToList()
            };
        }
    }
}
