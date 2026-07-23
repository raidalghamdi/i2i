namespace InnovationToImpact.Domain.Email;

public sealed record EmailOutboxProcessingResult(int Processed, int Sent, int Failed);
