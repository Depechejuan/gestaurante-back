using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Gestaurante.Models.Services
{
    public class CloudinaryEmployeeImageService : IEmployeeImageService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _cloudName;
        private readonly string? _apiKey;
        private readonly string? _apiSecret;
        private readonly string _employeeFolder;

        public CloudinaryEmployeeImageService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")
                ?? Environment.GetEnvironmentVariable("CLOUDINARY_CLOUDNAME");
            _apiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")
                ?? Environment.GetEnvironmentVariable("CLOUDINARY_APIKEY");
            _apiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
                ?? Environment.GetEnvironmentVariable("CLOUDINARY_APISECRET");
            _employeeFolder = Environment.GetEnvironmentVariable("CLOUDINARY_EMPLOYEE_FOLDER") ?? "gestaurante/empleados";
        }

        public async Task<string> UploadOrReplaceEmployeeImageAsync(Guid employeeId, IFormFile photo, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_cloudName) || string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_apiSecret))
                throw new InvalidOperationException("Cloudinary no está configurado. Revisa CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY y CLOUDINARY_API_SECRET.");


            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var publicId = $"{_employeeFolder.TrimEnd('/')}/{employeeId}";
            var parameters = new SortedDictionary<string, string>
            {
                ["overwrite"] = "true",
                ["public_id"] = publicId,
                ["timestamp"] = timestamp
            };

            var signature = BuildSignature(parameters, _apiSecret);
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(_apiKey), "api_key");
            content.Add(new StringContent(timestamp), "timestamp");
            content.Add(new StringContent(publicId), "public_id");
            content.Add(new StringContent("true"), "overwrite");
            content.Add(new StringContent(signature), "signature");

            await using var stream = photo.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(photo.ContentType);
            content.Add(fileContent, "file", photo.FileName);

            var endpoint = $"https://api.cloudinary.com/v1_1/{_cloudName}/image/upload";
            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Error subiendo imagen a Cloudinary: {payload}");

            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("secure_url", out var secureUrl))
                throw new InvalidOperationException("Cloudinary no devolvió secure_url para la imagen subida.");

            return secureUrl.GetString() ?? throw new InvalidOperationException("La URL de Cloudinary llegó vacía.");
        }

        private static string BuildSignature(SortedDictionary<string, string> parameters, string apiSecret)
        {
            var signatureBase = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}")) + apiSecret;
            var hash = SHA1.HashData(Encoding.UTF8.GetBytes(signatureBase));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
