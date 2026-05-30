using Djvrstl.Backend.Api;
using Microsoft.Extensions.Options;

namespace Djvrstl.Backend.Application.Admin;

public interface IProductImageStorageService
{
    Task<ProductImageUploadResponse> SaveAsync(HttpRequest request, IFormFileCollection files, CancellationToken cancellationToken);
}

public sealed class ProductImageStorageService(
    IWebHostEnvironment environment,
    IOptions<ProductImageUploadOptions> options) : IProductImageStorageService
{
    private readonly ProductImageUploadOptions _options = options.Value;

    public async Task<ProductImageUploadResponse> SaveAsync(
        HttpRequest request,
        IFormFileCollection files,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            throw new ProductImageUploadException("At least one image file is required.");
        }

        if (files.Count > _options.MaxFiles)
        {
            throw new ProductImageUploadException($"Upload up to {_options.MaxFiles} images at a time.");
        }

        var storagePath = ResolveStoragePath();
        Directory.CreateDirectory(storagePath);

        var images = new List<string>();
        foreach (var file in files)
        {
            Validate(file);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(storagePath, fileName);

            await using var stream = File.Create(fullPath);
            await file.CopyToAsync(stream, cancellationToken);

            images.Add(BuildPublicUrl(request, fileName));
        }

        return new ProductImageUploadResponse(images.ToArray());
    }

    private void Validate(IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new ProductImageUploadException("Image files cannot be empty.");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            throw new ProductImageUploadException("Image file is larger than the configured limit.");
        }

        if (!_options.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ProductImageUploadException("Image file type is not supported.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ProductImageUploadException("Image file extension is not supported.");
        }
    }

    private string ResolveStoragePath()
    {
        return Path.IsPathRooted(_options.StoragePath)
            ? _options.StoragePath
            : Path.Combine(environment.ContentRootPath, _options.StoragePath);
    }

    private string BuildPublicUrl(HttpRequest request, string fileName)
    {
        var requestPath = _options.RequestPath.TrimEnd('/');
        return $"{request.Scheme}://{request.Host}{request.PathBase}{requestPath}/{fileName}";
    }
}

public sealed class ProductImageUploadException(string message) : Exception(message);
