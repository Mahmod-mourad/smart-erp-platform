using System.Text;
using System.Text.RegularExpressions;

namespace NexaFlow.Infrastructure.Services;

public static partial class SlugGenerator
{
    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugChars();

    /// <summary>Turns "Acme Corp!" into "acme-corp".</summary>
    public static string Slugify(string input)
    {
        var lower = input.Trim().ToLowerInvariant();
        var slug = NonSlugChars().Replace(lower, "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "tenant" : slug;
    }

    /// <summary>Appends a short suffix to keep a slug unique.</summary>
    public static string WithSuffix(string baseSlug) =>
        $"{baseSlug}-{Guid.NewGuid().ToString("N")[..6]}";
}
