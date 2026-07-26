namespace InnovationToImpact.Domain.Entities;

/// <summary>
/// One ordered, typed, admin-editable block of the public homepage (landing page). ContentJson
/// carries a bilingual (ar/en) structured payload whose shape depends on Type -- see the contract
/// documented alongside HomePageSeeder.
/// </summary>
public class HomePageSection
{
    public Guid Id { get; set; }

    /// <summary>0-based display order.</summary>
    public int Idx { get; set; }

    public string Type { get; set; } = string.Empty;

    public bool IsVisible { get; set; } = true;

    public string ContentJson { get; set; } = "{}";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? UpdatedById { get; set; }
}
