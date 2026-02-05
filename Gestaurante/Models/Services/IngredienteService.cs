using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
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
        public async Task<List<Ingrediente>> GetAll()
        {
            return await _db.Ingredientes.ToListAsync();
        }
        public async Task CreateIngrediente(IngredienteDTO dto)
        {
            var ingrediente = new Ingrediente(
                Guid.NewGuid(),
                dto.Nombre,
                dto.Alergenico,
                dto.Disponible,
                dto.Imagen
            );
            await _db.Ingredientes.AddAsync(ingrediente);
            await _db.SaveChangesAsync();
        }
        public async Task CreateIngrediente(IngredienteDTO[] dto)
        {
            for (int i = 0; i < dto.Length; i++)
            {
                var ingrediente = new Ingrediente(
                    Guid.NewGuid(),
                    dto[i].Nombre,
                    dto[i].Alergenico,
                    dto[i].Disponible,
                    dto[i].Imagen
                );
                await _db.Ingredientes.AddAsync(ingrediente);
            }
             await _db.SaveChangesAsync();
        }
        public async Task DeleteIngrediente(Guid idIngrediente)
        {
            var ingrediente = await _db.Ingredientes.FindAsync(idIngrediente);
            if (ingrediente == null)
            {
                throw new Exception("Ingrediente no encontrado");
            }
            _db.Ingredientes.Remove(ingrediente);
            await _db.SaveChangesAsync();
        }
        public async Task UpdateIngrediente(Guid idIngrediente, IngredienteDTO dto)
        {
             var ingrediente = await _db.Ingredientes.FindAsync(idIngrediente);
            if (ingrediente == null)
            {
                throw new Exception("Ingrediente no encontrado");
            }
            ingrediente.Nombre = dto.Nombre;
            ingrediente.Alergenico = dto.Alergenico;
            ingrediente.Disponible = dto.Disponible;
            ingrediente.Imagen = dto.Imagen;
            ingrediente.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
