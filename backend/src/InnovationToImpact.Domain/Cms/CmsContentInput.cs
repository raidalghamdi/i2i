namespace InnovationToImpact.Domain.Cms;

public sealed record CmsContentInput(string Slug, string TitleAr, string TitleEn, string BodyAr, string BodyEn, bool IsPublished);
