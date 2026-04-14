using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class CategoriaService
    {
        private readonly AppDbContext _db;

        public CategoriaService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<CategoriaDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Categorias
                .AsNoTracking()
                .OrderBy(c => c.Descripcion)
                .Select(c => new CategoriaDTO
                {
                    IdCategoria = c.IdCategoria,
                    Descripcion = c.Descripcion
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<CategoriaDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Categorias
                .AsNoTracking()
                .Where(c => c.IdCategoria == id)
                .Select(c => new CategoriaDTO
                {
                    IdCategoria = c.IdCategoria,
                    Descripcion = c.Descripcion
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<CategoriaDTO> CreateAsync(CategoriaDTO dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Descripcion))
                throw new ValidationException("La descripcion de la categoria es obligatoria.");

            var descripcion = dto.Descripcion.Trim();
            var exists = await _db.Categorias.AnyAsync(c => c.Descripcion.ToLower() == descripcion.ToLower(), cancellationToken);
            if (exists)
                throw new InvalidOperationException("Ya existe una categoria con esa descripcion.");

            var categoria = new Categoria(Guid.NewGuid(), descripcion);
            await _db.Categorias.AddAsync(categoria, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return new CategoriaDTO
            {
                IdCategoria = categoria.IdCategoria,
                Descripcion = categoria.Descripcion
            };
        }

        public async Task<CategoriaDTO?> UpdateAsync(Guid id, CategoriaDTO dto, CancellationToken cancellationToken = default)
        {
            var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.IdCategoria == id, cancellationToken);
            if (categoria == null)
                return null;

            if (string.IsNullOrWhiteSpace(dto.Descripcion))
                throw new ValidationException("La descripcion de la categoria es obligatoria.");

            var descripcion = dto.Descripcion.Trim();
            var duplicate = await _db.Categorias.AnyAsync(
                c => c.IdCategoria != id && c.Descripcion.ToLower() == descripcion.ToLower(),
                cancellationToken);
            if (duplicate)
                throw new InvalidOperationException("Ya existe otra categoria con esa descripcion.");

            categoria.Descripcion = descripcion;
            await _db.SaveChangesAsync(cancellationToken);

            return new CategoriaDTO
            {
                IdCategoria = categoria.IdCategoria,
                Descripcion = categoria.Descripcion
            };
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.IdCategoria == id, cancellationToken);
            if (categoria == null)
                return false;

            var hasPlatos = await _db.Platos.AnyAsync(p => p.IdCategoria == id, cancellationToken);
            if (hasPlatos)
                throw new InvalidOperationException("No puedes borrar una categoria con platos asociados.");

            _db.Categorias.Remove(categoria);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
