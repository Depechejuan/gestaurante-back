using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Gestaurante.Configuration;
using Microsoft.Extensions.Options;

namespace Gestaurante.Models.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinaryOptions _options;

        public CloudinaryService(IOptions<CloudinaryOptions> options)
        {
            _options = options.Value;

            if (!_options.IsConfigured)
                throw new InvalidOperationException("Cloudinary no está configurado. Revisa CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY y CLOUDINARY_API_SECRET.");

            var account = new Account(
                _options.CloudName,
                _options.ApiKey,
                _options.ApiSecret
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(Guid id, IFormFile file, string location)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            await using var stream = file.OpenReadStream();
            string fileName = id.ToString();

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(fileName, stream),
                Folder = location
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            return result.SecureUrl.ToString();
        }

        public string ResolveImageUrl(string imagePath)
        {
            return _options.ResolveImageUrl(imagePath);
        }
    }

}
