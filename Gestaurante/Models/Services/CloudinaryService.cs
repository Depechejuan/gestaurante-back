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

            var publicId = id.ToString();
            await using var stream = file.OpenReadStream();
            var uploadParams = BuildUploadParams(publicId, location, new FileDescription(publicId, stream));

            var result = await _cloudinary.UploadAsync(uploadParams);
            return ResolveUploadedImageUrl(result, publicId);
        }

        public async Task<string> UploadImageAsync(Guid id, string remoteImageUrl, string location)
        {
            if (string.IsNullOrWhiteSpace(remoteImageUrl))
                return string.Empty;

            var publicId = id.ToString();
            var uploadParams = BuildUploadParams(publicId, location, new FileDescription(publicId, remoteImageUrl.Trim()));

            var result = await _cloudinary.UploadAsync(uploadParams);
            return ResolveUploadedImageUrl(result, publicId);
        }

        public string ResolveImageUrl(string imagePath)
        {
            return _options.ResolveImageUrl(imagePath);
        }

        private static ImageUploadParams BuildUploadParams(string publicId, string location, FileDescription file)
        {
            return new ImageUploadParams
            {
                File = file,
                Folder = location,
                PublicId = publicId,
                Overwrite = true
            };
        }

        private static string ResolveUploadedImageUrl(ImageUploadResult result, string publicId)
        {
            if (result.Error is not null)
                throw new InvalidOperationException($"Cloudinary devolvió un error al subir la imagen '{publicId}': {result.Error.Message}");

            if (result.SecureUrl is null)
                throw new InvalidOperationException($"Cloudinary no devolvió SecureUrl para la imagen '{publicId}'.");

            return result.SecureUrl.ToString();
        }
    }

}
