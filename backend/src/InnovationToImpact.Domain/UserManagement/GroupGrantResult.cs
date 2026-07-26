namespace InnovationToImpact.Domain.UserManagement;

public sealed record GroupGrantResult(int GrantedCount, int PendingCount, int SkippedCount, IReadOnlyList<string> Errors);
