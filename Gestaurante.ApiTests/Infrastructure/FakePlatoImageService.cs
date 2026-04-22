using Gestaurante.Models.Services;
using Microsoft.AspNetCore.Http;

namespace Gestaurante.ApiTests.Infrastructure;

public sealed class FakePlatoImageService : IPlatoImageService
{
    public const string CloudName = "test-cloud";

    private readonly List<FileUploadCall> _fileUploadCalls = [];
    private readonly List<RemoteUploadCall> _remoteUploadCalls = [];
    private readonly HashSet<string> _failingRemoteUrls = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<FileUploadCall> FileUploadCalls => _fileUploadCalls;
    public IReadOnlyList<RemoteUploadCall> RemoteUploadCalls => _remoteUploadCalls;

    public Task<string> UploadOrReplaceDishImageAsync(Guid dishId, IFormFile photo, CancellationToken cancellationToken = default)
    {
        var safeFileName = Path.GetFileNameWithoutExtension(photo.FileName ?? "dish-photo");
        var publicId = $"{dishId:N}-{safeFileName}.png";
        var finalUrl = BuildCloudinaryUrl(publicId);

        _fileUploadCalls.Add(new FileUploadCall(dishId, photo.FileName ?? "dish-photo", finalUrl));
        return Task.FromResult(finalUrl);
    }

    public Task<string> UploadOrReplaceDishImageFromUrlAsync(Guid dishId, string remoteImageUrl, CancellationToken cancellationToken = default)
    {
        var normalizedRemoteImageUrl = remoteImageUrl?.Trim() ?? string.Empty;
        if (_failingRemoteUrls.Contains(normalizedRemoteImageUrl))
            throw new InvalidOperationException("Simulated remote image upload failure.");

        var finalUrl = BuildCloudinaryUrl($"{dishId:N}{ResolveExtension(normalizedRemoteImageUrl)}");
        _remoteUploadCalls.Add(new RemoteUploadCall(dishId, normalizedRemoteImageUrl, finalUrl));
        return Task.FromResult(finalUrl);
    }

    public void FailRemoteUrl(string remoteImageUrl)
    {
        if (!string.IsNullOrWhiteSpace(remoteImageUrl))
            _failingRemoteUrls.Add(remoteImageUrl.Trim());
    }

    public void Clear()
    {
        _fileUploadCalls.Clear();
        _remoteUploadCalls.Clear();
        _failingRemoteUrls.Clear();
    }

    private static string BuildCloudinaryUrl(string publicId)
    {
        return $"https://res.cloudinary.com/{CloudName}/image/upload/gestaurante/platos/{publicId}";
    }

    private static string ResolveExtension(string remoteImageUrl)
    {
        if (!Uri.TryCreate(remoteImageUrl, UriKind.Absolute, out var uri))
            return ".jpg";

        var extension = Path.GetExtension(uri.AbsolutePath);
        return string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension;
    }

    public sealed record FileUploadCall(Guid DishId, string FileName, string FinalUrl);
    public sealed record RemoteUploadCall(Guid DishId, string SourceUrl, string FinalUrl);
}
