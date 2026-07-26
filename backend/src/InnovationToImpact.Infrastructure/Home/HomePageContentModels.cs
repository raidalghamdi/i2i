using System.Text.Json.Serialization;

namespace InnovationToImpact.Infrastructure.Home;

// Strongly-typed C# mirrors of the per-Type ContentJson schema documented in the C3/C4 contract
// (see scratchpad/sdd/home-section-contract.md). Every property carries an explicit
// [JsonPropertyName] so the emitted JSON keys are exact and don't depend on any global naming
// policy. These types exist purely to build the seed payloads via JsonSerializer.Serialize --
// admin edits after seeding go through the raw ContentJson string, not these types.

/// <summary>Bilingual text pair -- the "Loc" type referenced throughout the contract.</summary>
public sealed class Loc
{
    public Loc()
    {
    }

    public Loc(string ar, string en)
    {
        Ar = ar;
        En = en;
    }

    [JsonPropertyName("ar")]
    public string Ar { get; set; } = string.Empty;

    [JsonPropertyName("en")]
    public string En { get; set; } = string.Empty;
}

public sealed class HeroContent
{
    [JsonPropertyName("eyebrow")]
    public Loc Eyebrow { get; set; } = new();

    [JsonPropertyName("words")]
    public List<Loc> Words { get; set; } = new();

    [JsonPropertyName("headline")]
    public Loc Headline { get; set; } = new();

    [JsonPropertyName("subheadline")]
    public Loc Subheadline { get; set; } = new();

    [JsonPropertyName("primaryCtaLabel")]
    public Loc PrimaryCtaLabel { get; set; } = new();

    [JsonPropertyName("primaryCtaLink")]
    public string PrimaryCtaLink { get; set; } = string.Empty;

    [JsonPropertyName("secondaryCtaLabel")]
    public Loc SecondaryCtaLabel { get; set; } = new();

    [JsonPropertyName("secondaryCtaLink")]
    public string SecondaryCtaLink { get; set; } = string.Empty;

    [JsonPropertyName("closedNotice")]
    public Loc ClosedNotice { get; set; } = new();

    /// <summary>Extension beyond the brief's original hero shape -- see contract file.</summary>
    [JsonPropertyName("slogan")]
    public List<Loc> Slogan { get; set; } = new();
}

public sealed class AboutContent
{
    [JsonPropertyName("title")]
    public Loc Title { get; set; } = new();

    [JsonPropertyName("paragraphs")]
    public List<Loc> Paragraphs { get; set; } = new();

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = "";
}

public sealed class ObjectivesContent
{
    [JsonPropertyName("title")]
    public Loc Title { get; set; } = new();

    [JsonPropertyName("items")]
    public List<Loc> Items { get; set; } = new();
}

public sealed class TracksContent
{
    [JsonPropertyName("title")]
    public Loc Title { get; set; } = new();

    [JsonPropertyName("intro")]
    public Loc Intro { get; set; } = new();
}

/// <summary>
/// New type (beyond the brief's original "rules" shape): the html's "Program Details" section
/// bundles a Rules list, a Format paragraph, and an Eligibility paragraph together -- see contract.
/// </summary>
public sealed class DetailsContent
{
    [JsonPropertyName("title")]
    public Loc Title { get; set; } = new();

    [JsonPropertyName("rulesTitle")]
    public Loc RulesTitle { get; set; } = new();

    [JsonPropertyName("rules")]
    public List<Loc> Rules { get; set; } = new();

    [JsonPropertyName("formatTitle")]
    public Loc FormatTitle { get; set; } = new();

    [JsonPropertyName("format")]
    public Loc Format { get; set; } = new();

    [JsonPropertyName("eligibilityTitle")]
    public Loc EligibilityTitle { get; set; } = new();

    [JsonPropertyName("eligibility")]
    public Loc Eligibility { get; set; } = new();
}

public sealed class TimelineStageContent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public Loc Title { get; set; } = new();

    [JsonPropertyName("date")]
    public Loc Date { get; set; } = new();

    [JsonPropertyName("description")]
    public Loc Description { get; set; } = new();

    [JsonPropertyName("tone")]
    public string Tone { get; set; } = string.Empty;
}

public sealed class TimelineContent
{
    [JsonPropertyName("title")]
    public Loc Title { get; set; } = new();

    [JsonPropertyName("stages")]
    public List<TimelineStageContent> Stages { get; set; } = new();
}

public sealed class CriterionItemContent
{
    [JsonPropertyName("label")]
    public Loc Label { get; set; } = new();

    [JsonPropertyName("description")]
    public Loc Description { get; set; } = new();

    [JsonPropertyName("weight")]
    public int Weight { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;
}

public sealed class CriteriaContent
{
    [JsonPropertyName("title")]
    public Loc Title { get; set; } = new();

    /// <summary>Extension beyond the brief's original criteria shape -- see contract file.</summary>
    [JsonPropertyName("eyebrow")]
    public Loc Eyebrow { get; set; } = new();

    /// <summary>Extension beyond the brief's original criteria shape -- see contract file.</summary>
    [JsonPropertyName("lead")]
    public Loc Lead { get; set; } = new();

    [JsonPropertyName("items")]
    public List<CriterionItemContent> Items { get; set; } = new();
}

public sealed class PrizeItemContent
{
    [JsonPropertyName("tier")]
    public Loc Tier { get; set; } = new();

    [JsonPropertyName("value")]
    public Loc Value { get; set; } = new();
}

public sealed class PrizesContent
{
    [JsonPropertyName("title")]
    public Loc Title { get; set; } = new();

    [JsonPropertyName("items")]
    public List<PrizeItemContent> Items { get; set; } = new();
}

/// <summary>
/// Extended beyond the brief's original "title + items" shape: the html's "Previous Edition"
/// section bundles an intro paragraph, a gallery grid, AND a video sub-section -- see contract.
/// </summary>
public sealed class GalleryItem
{
    [JsonPropertyName("caption")]
    public Loc Caption { get; set; } = new();

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = "";
}

public sealed class GalleryContent
{
    [JsonPropertyName("title")]
    public Loc Title { get; set; } = new();

    [JsonPropertyName("body")]
    public Loc Body { get; set; } = new();

    [JsonPropertyName("galleryTitle")]
    public Loc GalleryTitle { get; set; } = new();

    [JsonPropertyName("items")]
    public List<GalleryItem> Items { get; set; } = new();

    [JsonPropertyName("videoTitle")]
    public Loc VideoTitle { get; set; } = new();

    [JsonPropertyName("videoHint")]
    public Loc VideoHint { get; set; } = new();

    [JsonPropertyName("videoUrl")]
    public string VideoUrl { get; set; } = "";
}

public sealed class PartnersContent
{
    [JsonPropertyName("title")]
    public Loc Title { get; set; } = new();

    [JsonPropertyName("items")]
    public List<Loc> Items { get; set; } = new();
}

public sealed class FaqItemContent
{
    [JsonPropertyName("q")]
    public Loc Q { get; set; } = new();

    [JsonPropertyName("a")]
    public Loc A { get; set; } = new();
}

public sealed class FaqContent
{
    [JsonPropertyName("title")]
    public Loc Title { get; set; } = new();

    [JsonPropertyName("items")]
    public List<FaqItemContent> Items { get; set; } = new();
}

public sealed class CtaContent
{
    [JsonPropertyName("title")]
    public Loc Title { get; set; } = new();

    [JsonPropertyName("subtitle")]
    public Loc Subtitle { get; set; } = new();

    [JsonPropertyName("buttonLabel")]
    public Loc ButtonLabel { get; set; } = new();

    [JsonPropertyName("buttonLink")]
    public string ButtonLink { get; set; } = string.Empty;
}
