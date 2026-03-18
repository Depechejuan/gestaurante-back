using Microsoft.AspNetCore.Http;

namespace Gestaurante.Models.Services
{
    public interface IEmployeeImageService
    {
        Task<string> UploadOrReplaceEmployeeImageAsync(Guid employeeId, IFormFile photo, CancellationToken cancellationToken = default);
    }
}
