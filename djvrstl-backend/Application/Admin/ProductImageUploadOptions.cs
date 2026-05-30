namespace Djvrstl.Backend.Application.Admin;

public sealed class ProductImageUploadOptions
{
    public const string SectionName = "ProductImageUploads";

    public string StoragePath { get; set; } = "wwwroot/uploads/products";
    public string RequestPath { get; set; } = "/uploads/products";
    public int MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
    public int MaxFiles { get; set; } = 8;
    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];
    public string[] AllowedExtensions { get; set; } =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    ];
}
