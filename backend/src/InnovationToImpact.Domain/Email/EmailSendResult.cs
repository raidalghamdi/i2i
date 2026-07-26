namespace InnovationToImpact.Domain.Email;

public sealed record EmailSendResult(bool Success, string Provider, string? ProviderMessageId, string? ErrorMessage);
