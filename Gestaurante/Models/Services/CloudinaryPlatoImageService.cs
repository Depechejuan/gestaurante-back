using Gestaurante.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Gestaurante.Models.Services
{
    public class CloudinaryPlatoImageService : IPlatoImageService
    {
        private readonly CloudinaryService _cloudinaryService;
        private readonly CloudinaryOptions _options;

        public CloudinaryPlatoImageService(CloudinaryService cloudinaryService, IOptions<CloudinaryOptions> options)
        {
            _cloudinaryService = cloudinaryService;
            _options = options.Value;
        }

        public async Task<string> UploadOrReplaceDishImageAsync(Guid dishId, IFormFile photo, CancellationToken cancellationToken = default)
        {
            return await _cloudinaryService.UploadImageAsync(dishId, photo, _options.DishFolder);
        }

        public async Task<string> UploadOrReplaceDishImageFromUrlAsync(Guid dishId, string remoteImageUrl, CancellationToken cancellationToken = default)
        {
            return await _cloudinaryService.UploadImageAsync(dishId, remoteImageUrl, _options.DishFolder);
        }
    }
}
