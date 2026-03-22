using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
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

        public async Task<List<Categoria>> GetAll()
        {
            return await _db.Categorias.ToListAsync();
        }

        public async Task<Categoria?> GetById(Guid id)
        {
            return await _db.Categorias.FindAsync(id);
        }

        public async Task Create(CategoriaDTO dto)
        {
            var categoria = new Categoria(Guid.NewGuid(), dto.Descripcion);
            await _db.Categorias.AddAsync(categoria);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> Update(CategoriaDTO dto)
        {
            var categoria = await _db.Categorias.FindAsync(dto.IdCategoria);
            if (categoria == null) return false;
            categoria.Descripcion = dto.Descripcion;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Delete(Guid id)
            {
            var categoria = await _db.Categorias.FindAsync(id);
            if (categoria == null) return false;
            _db.Categorias.Remove(categoria);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
