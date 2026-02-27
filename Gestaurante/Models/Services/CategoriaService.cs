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

        public async Task CreateCategoria(CategoriaDTO dto)
        {
            var categoria = new Categoria(dto.Descripcion);
            await _db.Categorias.AddAsync(categoria);
            await _db.SaveChangesAsync();
        }

        public async Task CreateCategoriaArray(CategoriaDTO[] dtoArray)
        {
            for (int i = 0; i < dtoArray.Length; i++)
            {
                var categoria = new Categoria(dtoArray[i].Descripcion);
                await _db.Categorias.AddAsync(categoria);
            }
            await _db.SaveChangesAsync();
        }
    }
}
