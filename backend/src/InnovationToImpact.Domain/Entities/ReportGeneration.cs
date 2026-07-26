namespace InnovationToImpact.Domain.Entities;

public class ReportGeneration
{
    public Guid Id { get; set; }

    public Guid ReportTitleId { get; set; }
    public ReportTitle ReportTitle { get; set; } = null!;

    public string Format { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; } = null!;

    public string? FileUrl { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
