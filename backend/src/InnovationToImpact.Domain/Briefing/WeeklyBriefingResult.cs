namespace InnovationToImpact.Domain.Briefing;

public sealed record WeeklyBriefingResult(
    int SlaBreachesThisWeek,
    int InvitationsAcceptedThisWeek,
    int PendingInvitations,
    int ExpiredInvitations,
    int AuditEntriesThisWeek,
    int RecipientsQueued);
