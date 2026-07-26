namespace InnovationToImpact.Domain.Home;

public static class HomeMediaRules
{
    public static readonly IReadOnlySet<string> AllowedImageContentTypes = new HashSet<string>
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif",
        "image/svg+xml",
    };

    public static readonly IReadOnlySet<string> AllowedVideoContentTypes = new HashSet<string>
    {
        "video/mp4",
        "video/webm",
    };

    public const long MaxImageSizeBytes = 5L * 1024 * 1024;
    public const long MaxVideoSizeBytes = 50L * 1024 * 1024;

    public static bool IsAllowed(string contentType) =>
        AllowedImageContentTypes.Contains(contentType) || AllowedVideoContentTypes.Contains(contentType);

    public static long MaxSizeFor(string contentType) =>
        AllowedVideoContentTypes.Contains(contentType) ? MaxVideoSizeBytes : MaxImageSizeBytes;
}
