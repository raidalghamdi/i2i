namespace InnovationToImpact.Domain.Ideas;

public static class IdeaAttachmentRules
{
    public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.ms-powerpoint",
        "image/png",
        "image/jpeg",
        "video/mp4",
        "video/quicktime",
    };

    public const long MaxSizeBytes = 10 * 1024 * 1024;
}
