using Microsoft.AspNetCore.Http;

namespace Gestaurante.Models.Services
{
    public interface IPlatoImageService
    {
        Task<string> UploadOrReplaceDishImageAsync(Guid dishId, IFormFile photo, CancellationToken cancellationToken = default);
        Task<string> UploadOrReplaceDishImageFromUrlAsync(Guid dishId, string remoteImageUrl, CancellationToken cancellationToken = default);
    }
}
