using Microsoft.AspNetCore.Http;

namespace Gestaurante.Models.Services
{
    public class CloudinaryEmployeeImageService : IEmployeeImageService
    {
        private readonly CloudinaryService _cloudinaryService;

        public CloudinaryEmployeeImageService(CloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        public async Task<string> UploadOrReplaceEmployeeImageAsync(Guid employeeId, IFormFile photo, CancellationToken cancellationToken = default)
        {
            return await _cloudinaryService.UploadImageAsync(employeeId, photo, "gestaurante/empleados");
        }
    }
}
