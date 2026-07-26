using System.Text.Json;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Home;

/// <summary>
/// Seeds the initial 12 homepage sections with the EXACT bilingual content of today's landing page
/// (frontend/src/app/landing/landing.component.html + .ts + frontend/src/locale/messages.ar.xlf),
/// so Task C4's Angular rewrite renders identically to what's live today. Only runs once: it's a
/// no-op as soon as any HomePageSection row exists. See scratchpad/sdd/home-section-contract.md for
/// the full schema this content conforms to and the section-order rationale.
/// </summary>
public static class HomePageSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public static async Task SeedIfMissingAsync(InnovationDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.HomePageSections.AnyAsync(cancellationToken)) return;

        db.HomePageSections.AddRange(BuildSeedSections());
        await db.SaveChangesAsync(cancellationToken);
    }

    public static List<HomePageSection> BuildSeedSections()
    {
        var now = DateTime.UtcNow;
        var idx = 0;

        HomePageSection Make<T>(string type, T content)
        {
            var section = new HomePageSection
            {
                Id = Guid.NewGuid(),
                Idx = idx,
                Type = type,
                IsVisible = true,
                ContentJson = JsonSerializer.Serialize(content, JsonOptions),
                UpdatedAt = now,
            };
            idx++;
            return section;
        }

        var hero = new HeroContent
        {
            Eyebrow = new Loc("برنامج ابتكار المنافسة", "Competition Innovation Program"),
            Words = new List<Loc>
            {
                new("ابتكر", "Innovate"),
                new("نافس", "Compete"),
                new("الأثر", "Impact"),
            },
            Headline = new Loc(string.Empty, string.Empty),
            Subheadline = new Loc(
                "فكرتك قد تُغيّر طريقة عمل المنافسة في السعودية — قدّمها أمام خبراء ومختصين، ونافس على جوائز قيّمة.",
                "Your idea could change how competition works in Saudi Arabia — present it before experts and specialists, and compete for valuable prizes."),
            PrimaryCtaLabel = new Loc("قدّم فكرتك", "Submit your idea"),
            PrimaryCtaLink = "/ideas/new",
            SecondaryCtaLabel = new Loc("تعرّف على البرنامج", "Learn about the program"),
            SecondaryCtaLink = "#about",
            ClosedNotice = new Loc("أُغلقت نافذة التقديم الحالية.", "The current submission window is closed."),
            Slogan = new List<Loc>
            {
                new("ابتكر", "Innovate"),
                new("نافس", "Compete"),
                new("أثر", "Impact"),
            },
        };

        var about = new AboutContent
        {
            Title = new Loc("عن البرنامج", "About the Program"),
            Paragraphs = new List<Loc>
            {
                new(
                    "برنامج المنافسة \"ابتكر لمنافس\" هو برنامج يجمع الموظفين ورواد الأعمال والجهات الحكومية لتحويل الأفكار المبتكرة إلى حلول عملية ذات أثر حقيقي على القطاع.",
                    "Innovation to Impact brings together employees, entrepreneurs, and government entities to turn innovative ideas into practical solutions with real impact on the sector."),
                new(
                    "مهمتنا: تسريع رحلة الفكرة من الإبداع إلى التنفيذ عبر منصة موحدة وشفافة.",
                    "Our mission: accelerate the journey from idea to execution through one unified, transparent platform."),
            },
            ImageUrl = "",
        };

        var objectives = new ObjectivesContent
        {
            Title = new Loc("أهداف البرنامج", "Objectives"),
            Items = new List<Loc>
            {
                new("تحفيز ثقافة الابتكار المؤسسي", "Foster a culture of institutional innovation"),
                new("تسريع تحويل الأفكار إلى مبادرات قابلة للتنفيذ", "Accelerate turning ideas into actionable initiatives"),
                new("تعزيز التعاون بين الفرق والجهات المختلفة", "Strengthen collaboration across teams and entities"),
                new("بناء قاعدة معرفية من الحلول القابلة لإعادة الاستخدام", "Build a reusable knowledge base of solutions"),
                new("قياس الأثر الفعلي للأفكار المنفذة", "Measure the real-world impact of implemented ideas"),
            },
        };

        var tracks = new TracksContent
        {
            Title = new Loc("المسارات والتحديات", "Tracks & Challenges"),
            Intro = new Loc(string.Empty, string.Empty),
        };

        var details = new DetailsContent
        {
            Title = new Loc("تفاصيل البرنامج", "Program Details"),
            RulesTitle = new Loc("القواعد", "Rules"),
            Rules = new List<Loc>
            {
                new("يمكن التسجيل بشكل فردي أو ضمن فريق لا يتجاوز 5 أعضاء", "Register individually or as a team of up to 5 members"),
                new("كل فكرة يجب أن تندرج تحت أحد المسارات المعتمدة", "Every idea must fall under one of the approved tracks"),
                new("يوقّع كل عضو من الفريق على شروط الملكية الفكرية بشكل مستقل", "Each team member signs the IP terms independently"),
            },
            FormatTitle = new Loc("الصيغة", "Format"),
            Format = new Loc(
                "مسابقة مفتوحة عبر منصة رقمية، تشمل مراحل تقديم وتقييم وعرض وقرار نهائي.",
                "An open competition on a digital platform, spanning submission, evaluation, pitching, and final decision stages."),
            EligibilityTitle = new Loc("شروط الأهلية", "Eligibility"),
            Eligibility = new Loc(
                "مفتوح لموظفي الهيئة ورواد الأعمال والجهات الحكومية والمتخصصين في الابتكار.",
                "Open to Authority employees, entrepreneurs, government entities, and innovation specialists."),
        };

        var timeline = new TimelineContent
        {
            Title = new Loc("الجدول الزمني", "Timeline"),
            Stages = new List<TimelineStageContent>
            {
                new()
                {
                    Id = "registration-open", Tone = "cyan",
                    Title = new Loc("فتح باب التسجيل", "Registration opens"),
                    Date = new Loc("١ أغسطس ٢٠٢٦", "1 August 2026"),
                    Description = new Loc("سجّل فرديًا أو ضمن فريق، واختر مسارك.", "Register individually or as a team, and choose your track."),
                },
                new()
                {
                    Id = "registration-close", Tone = "cyan",
                    Title = new Loc("إغلاق التسجيل", "Registration closes"),
                    Date = new Loc("١٥ سبتمبر ٢٠٢٦", "15 September 2026"),
                    Description = new Loc("آخر موعد لاستقبال طلبات المشاركة.", "Final deadline to receive participation requests."),
                },
                new()
                {
                    Id = "teams-announced", Tone = "cyan",
                    Title = new Loc("إعلان الفرق المقبولة", "Accepted teams announced"),
                    Date = new Loc("٢٠ سبتمبر ٢٠٢٦", "20 September 2026"),
                    Description = new Loc("فرز الطلبات واختيار الفرق.", "Applications are screened and teams selected."),
                },
                new()
                {
                    Id = "workshops", Tone = "cyan",
                    Title = new Loc("ورش التأهيل", "Qualification workshops"),
                    Date = new Loc("٢٥ سبت — ٨ أكت", "25 Sep — 8 Oct"),
                    Description = new Loc("ورش افتراضية في التفكير التصميمي.", "Virtual workshops in design thinking."),
                },
                new()
                {
                    Id = "hackathon", Tone = "gold",
                    Title = new Loc("أيام الهاكاثون · ٤٨ ساعة", "Hackathon days · 48 hours"),
                    Date = new Loc("١٢ — ١٣ أكتوبر", "12 — 13 October"),
                    Description = new Loc("ماراثون الابتكار الحضوري.", "The on-site innovation marathon."),
                },
                new()
                {
                    Id = "judging", Tone = "gold",
                    Title = new Loc("التحكيم", "Judging"),
                    Date = new Loc("١٤ أكتوبر — صباحًا", "14 October — morning"),
                    Description = new Loc("عرض الحلول أمام لجنة التحكيم.", "Solutions are presented to the judging panel."),
                },
                new()
                {
                    Id = "winners", Tone = "gold",
                    Title = new Loc("إعلان الفائزين", "Winners announced"),
                    Date = new Loc("١٤ أكتوبر — مساءً", "14 October — evening"),
                    Description = new Loc("حفل الختام والتتويج.", "The closing and awards ceremony."),
                },
            },
        };

        var criteria = new CriteriaContent
        {
            Title = new Loc("معايير التقييم", "Evaluation Criteria"),
            Eyebrow = new Loc("كيف نقيّم الأفكار", "How ideas are scored"),
            Lead = new Loc(
                "تمر كل فكرة على خمسة معايير اختيرت بعناية لتوزن الجرأة في الفكرة جنباً إلى جنب مع إمكانية تطبيقها وتأثيرها على السوق.",
                "Every submission is measured against five weighted criteria — balancing boldness of the idea with how well it can actually ship and shift the market."),
            Items = new List<CriterionItemContent>
            {
                new()
                {
                    Label = new Loc("الابتكار", "Innovation"),
                    Description = new Loc("جدّة الفكرة وتميّزها عمّا هو قائم في السوق.", "How novel and differentiated the idea is versus what already exists."),
                    Weight = 25, Color = "#01696F", Icon = "sparkles",
                },
                new()
                {
                    Label = new Loc("الأثر", "Impact"),
                    Description = new Loc("حجم القيمة المتوقّعة للمنافسة والمستهلك والاقتصاد.", "Expected value to competition, consumers, and the wider economy."),
                    Weight = 25, Color = "#20808D", Icon = "rocket",
                },
                new()
                {
                    Label = new Loc("قابلية التنفيذ", "Feasibility"),
                    Description = new Loc("وضوح الخطة وواقعية الموارد والجدول الزمني.", "Clarity of the plan, realism of the resources and the timeline."),
                    Weight = 20, Color = "#D19900", Icon = "wrench",
                },
                new()
                {
                    Label = new Loc("قابلية التوسع", "Scalability"),
                    Description = new Loc("إمكانية التوسّع جغرافيّاً أو قطاعيّاً دون إعادة بناء الفكرة.", "Ability to expand geographically or across sectors without rebuilding."),
                    Weight = 20, Color = "#A84B2F", Icon = "expand",
                },
                new()
                {
                    Label = new Loc("جودة العرض", "Presentation quality"),
                    Description = new Loc("دقة الملخّص وترابط الأدلة وجذب العرض للجنة التحكيم.", "Sharpness of the summary, strength of the evidence, and pitch quality."),
                    Weight = 10, Color = "#7A7974", Icon = "presentation",
                },
            },
        };

        var prizes = new PrizesContent
        {
            Title = new Loc("الجوائز", "Prizes"),
            Items = new List<PrizeItemContent>
            {
                new() { Tier = new Loc("المركز الأول", "First Place"), Value = new Loc("100,000 ريال + دعم تنفيذ الفكرة", "SAR 100,000 + implementation support") },
                new() { Tier = new Loc("المركز الثاني", "Second Place"), Value = new Loc("60,000 ريال", "SAR 60,000") },
                new() { Tier = new Loc("المركز الثالث", "Third Place"), Value = new Loc("30,000 ريال", "SAR 30,000") },
            },
        };

        var gallery = new GalleryContent
        {
            Title = new Loc("النسخة السابقة", "Previous Edition"),
            Body = new Loc(
                "شهدت النسخة السابقة من البرنامج مشاركة واسعة وأفكارًا تم تبنيها وتنفيذ عدد منها فعليًا.",
                "The previous edition saw wide participation, with several ideas adopted and actually implemented."),
            GalleryTitle = new Loc("من صور النسخة السابقة", "From the previous edition"),
            Items = new List<GalleryItem>
            {
                new() { Caption = new Loc("حفل الافتتاح", "Opening ceremony"), ImageUrl = "" },
                new() { Caption = new Loc("ورش العمل", "Workshops"), ImageUrl = "" },
                new() { Caption = new Loc("العروض التقديمية", "Pitches"), ImageUrl = "" },
                new() { Caption = new Loc("لجنة التحكيم", "Judging panel"), ImageUrl = "" },
                new() { Caption = new Loc("تكريم الفائزين", "Winners"), ImageUrl = "" },
                new() { Caption = new Loc("صورة جماعية", "Group photo"), ImageUrl = "" },
            },
            VideoTitle = new Loc("فيديو النسخة السابقة", "Previous edition video"),
            VideoHint = new Loc("سيتم إضافة الفيديو قريبًا", "The video will be added soon"),
            VideoUrl = "",
        };

        var partners = new PartnersContent
        {
            Title = new Loc("شركاء النجاح", "Success Partners"),
            Items = new List<Loc>
            {
                new("وزارة التجارة", "Ministry of Commerce"),
                new("منشآت", "Monsha'at (SME Authority)"),
                new("سدايا", "SDAIA"),
                new("مدينة الملك عبدالعزيز للعلوم والتقنية", "King Abdulaziz City for Science and Technology"),
                new("الهيئة السعودية للبيانات والذكاء الاصطناعي", "Saudi Data & AI Authority"),
                new("الجامعات المحلية", "Local Universities"),
                new("الغرف التجارية", "Private Sector Chambers"),
                new("حاضنات الابتكار", "Innovation Hubs"),
            },
        };

        var faq = new FaqContent
        {
            Title = new Loc("الأسئلة الشائعة", "Frequently Asked Questions"),
            Items = new List<FaqItemContent>
            {
                new() { Q = new Loc("من يمكنه تقديم فكرة؟", "Who can submit an idea?"), A = new Loc("يمكن لموظفي الهيئة ورواد الأعمال والجهات الحكومية ومختصي الابتكار تقديم أفكارهم.", "Employees of the Authority, entrepreneurs, government entities, and innovation specialists are all welcome to submit.") },
                new() { Q = new Loc("كم يستغرق التقييم؟", "How long does evaluation take?"), A = new Loc("الفحص الأولي: أيام عمل قليلة. التقييم الكامل: أسبوعان إلى ثلاثة.", "Initial check: a few working days. Full evaluation: two to three weeks.") },
                new() { Q = new Loc("هل تبقى فكرتي سرية؟", "Is my idea kept confidential?"), A = new Loc("نعم، تُعامَل الأفكار وفق مستوى السرية ولا يطّلع عليها سوى المقيّمين وأعضاء اللجنة المخوّلين.", "Yes. Ideas are handled according to the confidentiality level and reviewed only by authorized evaluators and committee members.") },
                new() { Q = new Loc("هل يمكنني تقديم أكثر من فكرة؟", "Can I submit more than one idea?"), A = new Loc("نعم، لا يوجد حد لعدد الأفكار التي يمكنك تقديمها.", "Yes, there is no limit on the number of ideas you may submit.") },
                new() { Q = new Loc("ماذا يحدث إذا احتاجت فكرتي إلى تعديل؟", "What happens if my idea needs changes?"), A = new Loc("ستصلك ملاحظات حول ما يحتاج إلى تحسين، ويمكنك تعديل فكرتك وإعادة تقديمها.", "You'll get feedback on what needs work and can update your idea and resubmit.") },
                new() { Q = new Loc("هل أحتفظ بملكية فكرتي؟", "Do I retain ownership of my idea?"), A = new Loc("توضّح شروط الملكية الفكرية التي تقرّها عند التقديم تفاصيل الملكية.", "Ownership terms are described in the IP terms you acknowledge when submitting.") },
                new() { Q = new Loc("هل سأُشعَر بالتقدّم؟", "Will I be notified of progress?"), A = new Loc("نعم، سيصلك إشعار في كل مرة يحدث فيها تطوّر على فكرتك.", "Yes — you'll get a notification every time something happens with your idea.") },
                new() { Q = new Loc("ما معايير التقييم؟", "What are the evaluation criteria?"), A = new Loc("تُقيَّم الأفكار وفق المواءمة الاستراتيجية والابتكار والجدوى والأثر والجهد. راجع صفحة معايير التقييم.", "Ideas are scored on strategic alignment, innovation, feasibility, impact, and effort. See the Evaluation Criteria page.") },
            },
        };

        var cta = new CtaContent
        {
            Title = new Loc("جاهز للمشاركة؟", "Ready to take part?"),
            Subtitle = new Loc("لا تُطل التفكير، فقط أخبرنا بما يدور في ذهنك.", "Don't overthink it — just tell us what you're thinking."),
            ButtonLabel = new Loc("قدّم فكرتك", "Submit your idea"),
            ButtonLink = "/ideas/new",
        };

        return new List<HomePageSection>
        {
            Make("hero", hero),
            Make("about", about),
            Make("objectives", objectives),
            Make("tracks", tracks),
            Make("details", details),
            Make("timeline", timeline),
            Make("criteria", criteria),
            Make("prizes", prizes),
            Make("gallery", gallery),
            Make("partners", partners),
            Make("faq", faq),
            Make("cta", cta),
        };
    }
}
