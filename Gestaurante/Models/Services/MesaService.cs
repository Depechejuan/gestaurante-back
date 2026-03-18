using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class MesaService
    {
        private readonly AppDbContext _db;

        public MesaService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<MesaDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var mesas = await _db.Mesas
                .AsNoTracking()
                .OrderBy(m => m.Ubicacion)
                .ThenBy(m => m.Capacidad)
                .ToListAsync(cancellationToken);

            return mesas.Select(MapMesa).ToList();
        }

        public async Task<MesaDTO?> GetByIdAsync(Guid idMesa, CancellationToken cancellationToken = default)
        {
            var mesa = await _db.Mesas
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdMesa == idMesa, cancellationToken);

            return mesa == null ? null : MapMesa(mesa);
        }

        public async Task<MesaDTO> CreateAsync(CrearMesaDTO dto, CancellationToken cancellationToken = default)
        {
            var mesa = new Mesa(Guid.NewGuid(), dto.Capacidad, dto.Estado, dto.Ubicacion.Trim());
            await _db.Mesas.AddAsync(mesa, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return MapMesa(mesa);
        }

        public async Task<MesaDTO?> UpdateAsync(Guid idMesa, EditarMesaDTO dto, CancellationToken cancellationToken = default)
        {
            var mesa = await _db.Mesas.FirstOrDefaultAsync(m => m.IdMesa == idMesa, cancellationToken);
            if (mesa == null) return null;

            if (dto.Capacidad.HasValue)
            {
                mesa.Capacidad = dto.Capacidad.Value;
            }

            if (dto.Estado.HasValue)
            {
                mesa.Estado = dto.Estado.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Ubicacion))
            {
                mesa.Ubicacion = dto.Ubicacion.Trim();
            }

            await _db.SaveChangesAsync(cancellationToken);
            return MapMesa(mesa);
        }

        public async Task<bool> DeleteAsync(Guid idMesa, CancellationToken cancellationToken = default)
        {
            var mesa = await _db.Mesas.FirstOrDefaultAsync(m => m.IdMesa == idMesa, cancellationToken);
            if (mesa == null) return false;

            _db.Mesas.Remove(mesa);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static MesaDTO MapMesa(Mesa mesa)
        {
            return new MesaDTO
            {
                IdMesa = mesa.IdMesa,
                Capacidad = mesa.Capacidad,
                Estado = mesa.Estado,
                Ubicacion = mesa.Ubicacion
            };
        }
    }
}
