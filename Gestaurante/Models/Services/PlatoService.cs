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
            // Incluye los ingredientes relacionados
            return await _db.Platos
                .Include(p => p.PlatoIngredientes)
                .ToListAsync();
        }
        public async Task CreatePlato(PlatoDTO dto)
        {
            var categoria = await _db.Categorias.FindAsync(dto.IdCategoria);
            if (categoria == null)
                throw new Exception("Categoría no encontrada");

            var plato = new Plato(
                dto.IdPlato == Guid.Empty ? Guid.NewGuid() : dto.IdPlato,
                dto.Nombre,
                dto.Descripcion,
                dto.Imagen,
                dto.Disponible,
                dto.Precio,
                categoria 
            );

            // Asignar ingredientes
            foreach (var ing in dto.Ingredientes)
            {
                plato.PlatoIngredientes.Add(new PlatoIngrediente
                {
                    IdPlato = plato.IdPlato,
                    IdIngrediente = ing.IdIngrediente
                });
            }

            await _db.Platos.AddAsync(plato);
            await _db.SaveChangesAsync();
        }
    }
}
