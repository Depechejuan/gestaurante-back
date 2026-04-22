using Gestaurante.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Gestaurante.Models.Services
{
    public class CloudinaryEmployeeImageService : IEmployeeImageService
    {
        private readonly CloudinaryService _cloudinaryService;
        private readonly CloudinaryOptions _options;

        public CloudinaryEmployeeImageService(CloudinaryService cloudinaryService, IOptions<CloudinaryOptions> options)
        {
            _cloudinaryService = cloudinaryService;
            _options = options.Value;
        }

        public async Task<string> UploadOrReplaceEmployeeImageAsync(Guid employeeId, IFormFile photo, CancellationToken cancellationToken = default)
        {
            return await _cloudinaryService.UploadImageAsync(employeeId, photo, _options.EmployeeFolder);
        }
    }
}
