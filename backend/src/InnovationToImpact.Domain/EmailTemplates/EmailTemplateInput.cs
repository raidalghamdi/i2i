namespace InnovationToImpact.Domain.EmailTemplates;

public sealed record EmailTemplateInput(string SubjectAr, string SubjectEn, string BodyAr, string BodyEn, bool IsBroadcast);
