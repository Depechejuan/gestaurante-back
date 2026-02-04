using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;

namespace Gestaurante.Models.Services
{
    public class IngredienteService
    {
        private readonly AppDbContext _db;

        public IngredienteService(AppDbContext db)
        {
            _db = db;
        }
        public List<Ingrediente> GetAll()
        {
            return _db.Ingredientes.ToList();
            
        }
        public Ingrediente CreateIngrediente(IngredienteDTO dto)
        {
            var ingrediente = new Ingrediente(
                Guid.NewGuid(),
                dto.Nombre,
                dto.Alergenico,
                dto.Disponible,
                dto.Imagen
            );
            _db.Ingredientes.Add(ingrediente);
            _db.SaveChanges();
            return ingrediente;
        }
        public void CreateIngrediente(IngredienteDTO[] dto)
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
                _db.Ingredientes.Add(ingrediente);
            }
            _db.SaveChanges();
        }
        public void DeleteIngrediente(Guid idIngrediente)
        {
            var ingrediente = _db.Ingredientes.Find(idIngrediente);
            if (ingrediente == null)
            {
                throw new Exception("Ingrediente no encontrado");
            }
            _db.Ingredientes.Remove(ingrediente);
            _db.SaveChanges();
        }
        public void UpdateIngrediente(Guid idIngrediente, IngredienteDTO dto)
        {
            var ingrediente =  _db.Ingredientes.Find(idIngrediente);
            if (ingrediente == null)
            {
                throw new Exception("Ingrediente no encontrado");
            }
            ingrediente.Nombre = dto.Nombre;
            ingrediente.Alergenico = dto.Alergenico;
            ingrediente.Disponible = dto.Disponible;
            ingrediente.Imagen = dto.Imagen;
            ingrediente.UpdatedAt = DateTime.UtcNow;
            _db.SaveChanges();
        }
    }
}
