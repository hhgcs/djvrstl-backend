using Djvrstl.Backend.Domain;

namespace Djvrstl.Backend.Infrastructure;

public sealed class SeedDataOptions
{
    public const string SectionName = "SeedData";

    public bool Enabled { get; set; }
    public Product[] Products { get; set; } = [];
}
