using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    /// <summary>
    /// Gestiona la administración interna del catálogo de platos.
    /// </summary>
    public class PlatoService
    {
        private readonly AppDbContext _db;
        private readonly CatalogProjectionService _catalogProjectionService;
        private readonly IPlatoImageService _platoImageService;

        /// <summary>
        /// Inicializa el servicio de platos con acceso a persistencia y proyección de catálogo.
        /// </summary>
        /// <param name="db">Contexto de base de datos del dominio.</param>
        /// <param name="catalogProjectionService">Servicio de proyección de platos a DTOs.</param>
        public PlatoService(AppDbContext db, CatalogProjectionService catalogProjectionService, IPlatoImageService platoImageService)
        {
            _db = db;
            _catalogProjectionService = catalogProjectionService;
            _platoImageService = platoImageService;
        }

        /// <summary>
        /// Recupera todos los platos del catálogo interno con su categoría e ingredientes.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Listado completo de platos para administración interna.</returns>
        public async Task<List<PlatoDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var platos = await _db.Platos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.PlatoIngredientes)
                    .ThenInclude(pi => pi.Ingrediente)
                .OrderBy(p => p.Nombre)
                .ToListAsync(cancellationToken);

            return platos.Select(_catalogProjectionService.MapInternal).ToList();
        }

        /// <summary>
        /// Recupera un plato concreto por identificador.
        /// </summary>
        /// <param name="id">Identificador del plato.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Plato solicitado o <see langword="null"/> si no existe.</returns>
        public async Task<PlatoDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var plato = await _db.Platos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.PlatoIngredientes)
                    .ThenInclude(pi => pi.Ingrediente)
                .FirstOrDefaultAsync(p => p.IdPlato == id, cancellationToken);

            return plato == null ? null : _catalogProjectionService.MapInternal(plato);
        }

        /// <summary>
        /// Crea un nuevo plato y vincula sus ingredientes seleccionados.
        /// </summary>
        /// <param name="dto">Datos del nuevo plato.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Plato creado ya proyectado a DTO.</returns>
        public async Task<PlatoDTO> CreateAsync(PlatoDTO dto, CancellationToken cancellationToken = default)
        {
            ValidatePlatoInput(dto);

            var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.IdCategoria == dto.IdCategoria, cancellationToken);
            if (categoria == null)
                throw new ValidationException("La categoria indicada no existe.");

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

        /// <summary>
        /// Actualiza los datos principales y las relaciones de ingredientes de un plato existente.
        /// </summary>
        /// <param name="id">Identificador del plato a actualizar.</param>
        /// <param name="dto">Datos actualizados del plato.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Plato actualizado o <see langword="null"/> si no existe.</returns>
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
                throw new ValidationException("La categoria indicada no existe.");

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

        /// <summary>
        /// Elimina físicamente un plato solo cuando no tiene histórico de pedidos.
        /// </summary>
        /// <param name="id">Identificador del plato a eliminar.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns><see langword="true"/> si el plato se elimina; en otro caso, <see langword="false"/>.</returns>
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

        /// <summary>
        /// Activa o desactiva la disponibilidad operativa de un plato sin borrar su histórico.
        /// </summary>
        /// <param name="id">Identificador del plato.</param>
        /// <param name="disponible">Nuevo estado de disponibilidad pública del plato.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Plato actualizado o <see langword="null"/> si no existe.</returns>
        public async Task<PlatoDTO?> SetDisponibilidadAsync(Guid id, bool disponible, CancellationToken cancellationToken = default)
        {
            var plato = await _db.Platos
                .FirstOrDefaultAsync(p => p.IdPlato == id, cancellationToken);

            if (plato == null)
                return null;

            plato.Disponible = disponible;
            plato.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return await GetByIdAsync(id, cancellationToken);
        }

        /// <summary>
        /// Sube o reemplaza la imagen de un plato usando Cloudinary y persiste la URL final en base de datos.
        /// </summary>
        public async Task<PlatoDTO?> SetImageAsync(Guid id, IFormFile photo, CancellationToken cancellationToken = default)
        {
            var plato = await _db.Platos
                .FirstOrDefaultAsync(p => p.IdPlato == id, cancellationToken);

            if (plato == null)
                return null;

            plato.Imagen = await _platoImageService.UploadOrReplaceDishImageAsync(id, photo, cancellationToken);
            plato.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return await GetByIdAsync(id, cancellationToken);
        }

        /// <summary>
        /// Carga y valida la lista de ingredientes asociados al plato.
        /// </summary>
        private async Task<List<Ingrediente>> LoadIngredientesAsync(List<Guid> ingredienteIds, CancellationToken cancellationToken)
        {
            if (ingredienteIds.Count == 0)
                return new List<Ingrediente>();

            var ingredientes = await _db.Ingredientes
                .Where(i => ingredienteIds.Contains(i.IdIngrediente))
                .ToListAsync(cancellationToken);

            if (ingredientes.Count != ingredienteIds.Count)
                throw new ValidationException("Uno o varios ingredientes indicados no existen.");

            return ingredientes;
        }

        /// <summary>
        /// Normaliza la lista de ingredientes recibida para evitar duplicados o identificadores vacíos.
        /// </summary>
        private static List<Guid> NormalizeIngredienteIds(List<PlatoIngredienteDTO>? ingredientes)
        {
            return (ingredientes ?? new List<PlatoIngredienteDTO>())
                .Select(i => i.IdIngrediente)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Valida las reglas mínimas de negocio necesarias para crear o editar un plato.
        /// </summary>
        private static void ValidatePlatoInput(PlatoDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new ValidationException("El nombre del plato es obligatorio.");

            if (string.IsNullOrWhiteSpace(dto.Descripcion))
                throw new ValidationException("La descripcion del plato es obligatoria.");

            if (dto.IdCategoria == Guid.Empty)
                throw new ValidationException("Debes indicar una categoria valida.");

            if (dto.Precio < 0)
                throw new ValidationException("El precio del plato no puede ser negativo.");
        }
    }
}
