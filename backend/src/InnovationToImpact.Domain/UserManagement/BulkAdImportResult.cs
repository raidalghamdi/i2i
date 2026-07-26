namespace InnovationToImpact.Domain.UserManagement;

/// <summary>
/// Outcome of a bulk "import all users from AD as submitters" run. Only accounts with no roles are
/// touched: an existing user with zero roles is granted submitter; a never-logged-in person gets a
/// pending submitter grant. Everyone else is left untouched and reported as skipped.
/// </summary>
public sealed record BulkAdImportResult(
    int Total,
    int Granted,
    int Pending,
    int SkippedHasRoles,
    int SkippedAlreadyPending);
