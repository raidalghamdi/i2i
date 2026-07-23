namespace InnovationToImpact.Domain.Entities;

public class IpRecord
{
    public Guid Id { get; set; }

    public Guid? IdeaId { get; set; }
    public Idea? Idea { get; set; }

    public Guid IpTypeId { get; set; }
    public IpType IpType { get; set; } = null!;

    public string OwnershipPartyAr { get; set; } = string.Empty;
    public string OwnershipPartyEn { get; set; } = string.Empty;
    public string ConfidentialityTermsAr { get; set; } = string.Empty;
    public string ConfidentialityTermsEn { get; set; } = string.Empty;
    public string ParticipationConditionsAr { get; set; } = string.Empty;
    public string ParticipationConditionsEn { get; set; } = string.Empty;

    public bool NdaRequired { get; set; }
    public DateTime? NdaSignedAt { get; set; }
}
