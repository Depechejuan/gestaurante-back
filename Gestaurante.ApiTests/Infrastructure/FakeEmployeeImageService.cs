using Gestaurante.Models.Services;
using Microsoft.AspNetCore.Http;

namespace Gestaurante.ApiTests.Infrastructure;

public sealed class FakeEmployeeImageService : IEmployeeImageService
{
    public Task<string> UploadOrReplaceEmployeeImageAsync(Guid employeeId, IFormFile photo, CancellationToken cancellationToken = default)
    {
        var safeFileName = Path.GetFileNameWithoutExtension(photo.FileName ?? "employee-photo");
        return Task.FromResult($"gestaurante/tests/{employeeId:N}/{safeFileName}.png");
    }
}
