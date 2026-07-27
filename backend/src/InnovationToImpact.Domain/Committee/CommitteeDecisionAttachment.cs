using System.Text.Json;

namespace InnovationToImpact.Domain.Committee;

// Change 20260726
public sealed record CommitteeDecisionAttachmentUpload(string FileName, string ContentType, byte[] Content);

// Change 20260726
// Serialized into CommitteeDecision.AttachmentsJson. `Id` is what the download endpoint addresses;
// `StoredPath` is an internal blob path and is never returned to clients.
public sealed record CommitteeDecisionAttachment
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string StoredPath { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTime UploadedAt { get; init; }

    public static IReadOnlyList<CommitteeDecisionAttachment> Parse(string? attachmentsJson)
    {
        if (string.IsNullOrWhiteSpace(attachmentsJson)) return Array.Empty<CommitteeDecisionAttachment>();
        return JsonSerializer.Deserialize<List<CommitteeDecisionAttachment>>(attachmentsJson) ?? new List<CommitteeDecisionAttachment>();
    }
}
