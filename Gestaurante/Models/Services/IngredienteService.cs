using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class IngredienteService
    {
        private readonly AppDbContext _db;

        public IngredienteService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<IngredienteDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Ingredientes
                .AsNoTracking()
                .OrderBy(i => i.Nombre)
                .Select(i => MapIngrediente(i))
                .ToListAsync(cancellationToken);
        }

        public async Task<IngredienteDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Ingredientes
                .AsNoTracking()
                .Where(i => i.IdIngrediente == id)
                .Select(i => MapIngrediente(i))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IngredienteDTO> CreateAsync(IngredienteDTO dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new ValidationException("El nombre del ingrediente es obligatorio.");


            var nombre = dto.Nombre.Trim();
            var exists = await _db.Ingredientes.AnyAsync(i => i.Nombre.ToLower() == nombre.ToLower(), cancellationToken);
            if (exists)
                throw new InvalidOperationException("Ya existe un ingrediente con ese nombre.");


            var ingrediente = new Ingrediente(Guid.NewGuid(), nombre, dto.Alergenico, dto.Disponible, dto.Imagen?.Trim() ?? string.Empty);
            await _db.Ingredientes.AddAsync(ingrediente, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return MapIngrediente(ingrediente);
        }

        public async Task<IngredienteDTO?> UpdateAsync(Guid id, IngredienteDTO dto, CancellationToken cancellationToken = default)
        {
            var ingrediente = await _db.Ingredientes.FirstOrDefaultAsync(i => i.IdIngrediente == id, cancellationToken);
            if (ingrediente == null)
                return null;


            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new ValidationException("El nombre del ingrediente es obligatorio.");


            var nombre = dto.Nombre.Trim();
            var duplicate = await _db.Ingredientes.AnyAsync(
                i => i.IdIngrediente != id && i.Nombre.ToLower() == nombre.ToLower(),
                cancellationToken);
            if (duplicate)
                throw new InvalidOperationException("Ya existe otro ingrediente con ese nombre.");


            ingrediente.Nombre = nombre;
            ingrediente.Alergenico = dto.Alergenico;
            ingrediente.Disponible = dto.Disponible;
            ingrediente.Imagen = dto.Imagen?.Trim() ?? string.Empty;
            ingrediente.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return MapIngrediente(ingrediente);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var ingrediente = await _db.Ingredientes.FirstOrDefaultAsync(i => i.IdIngrediente == id, cancellationToken);
            if (ingrediente == null)
                return false;

            _db.Ingredientes.Remove(ingrediente);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static IngredienteDTO MapIngrediente(Ingrediente ingrediente)
        {
            return new IngredienteDTO
            {
                IdIngrediente = ingrediente.IdIngrediente,
                Nombre = ingrediente.Nombre,
                Alergenico = ingrediente.Alergenico,
                Disponible = ingrediente.Disponible,
                Imagen = ingrediente.Imagen,
                CreatedAt = ingrediente.CreatedAt,
                UpdatedAt = ingrediente.UpdatedAt
            };
        }
    }
}
