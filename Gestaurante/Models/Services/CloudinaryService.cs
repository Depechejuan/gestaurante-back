using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Gestaurante.Models.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            var account = new Account(
                Environment.GetEnvironmentVariable("CLOUDINARY_CLOUDNAME"),
                Environment.GetEnvironmentVariable("CLOUDINARY_APIKEY"),
                Environment.GetEnvironmentVariable("CLOUDINARY_APISECRET")
            );

            _cloudinary = new Cloudinary(account);
        }

        // Incluímos la localización, así la carpeta será siempre esa :)
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
    }

}
