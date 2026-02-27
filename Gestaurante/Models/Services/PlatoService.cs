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

        public async Task<List<Plato>> GetAll()
        {
            return await _db.Platos.ToListAsync();
        }
        public async Task CreatePlato(PlatoDTO dto)
        {
                var plato = new Plato(
                    dto.IdPlato,
                    dto.Nombre,
                    dto.Descripcion,
                    dto.Imagen,
                    dto.Categoria,
                    dto.Disponible,
                    dto.Precio
                    
                );
                await _db.Platos.AddAsync(plato);
                await _db.SaveChangesAsync();
        }
    }
}
