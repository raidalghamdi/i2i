namespace InnovationToImpact.Domain.Reports.Bundle;

public sealed record ReportMeta(string NameEn, string NameAr, string DescEn, string DescAr);

public static class ReportCatalog
{
    private static readonly IReadOnlyDictionary<string, ReportMeta> Meta_ = new Dictionary<string, ReportMeta>
    {
        [ReportTypeCodes.Executive] = new(
            "Executive Performance Overview",
            "التقرير التنفيذي",
            "Board-ready summary: idea volume, approval rate, and projected value for the period.",
            "ملخص تنفيذي للقيادة العليا: حجم الأفكار، معدل الاعتماد، والأثر المتوقع خلال الفترة."),
        [ReportTypeCodes.Detailed] = new(
            "Comprehensive Detailed Report",
            "التقرير التفصيلي الشامل",
            "Record-level review of all submitted ideas, including team composition and current status.",
            "استعراض كامل على مستوى السجل لجميع الأفكار المُقدَّمة، مع بيانات الفريق والحالة."),
        [ReportTypeCodes.Media] = new(
            "Media & Corporate Communications Report",
            "تقرير الإعلام والاتصال المؤسسي",
            "Publication-ready brief highlighting featured stories, approved quotations, and headline metrics.",
            "ملخص جاهز للنشر يبرز أبرز القصص، الاقتباسات المعتمدة، والمؤشرات الرئيسية."),
        [ReportTypeCodes.Cx] = new(
            "Innovator Experience Report",
            "تقرير تجربة المُبتكِر",
            "Satisfaction indicators, response times, and voice-of-innovator analysis.",
            "مؤشرات رضا المستفيدين، أزمنة الاستجابة، وتحليل صوت المُبتكِر."),
        [ReportTypeCodes.Operational] = new(
            "Operational Performance Report",
            "التقرير التشغيلي",
            "Operational KPIs: workload distribution, SLA compliance, and assignment integrity.",
            "مؤشرات الأداء التشغيلي: أحمال العمل، الالتزام باتفاقيات مستوى الخدمة، وسلامة الإسناد."),
        [ReportTypeCodes.Audit] = new(
            "Audit & Compliance Report",
            "تقرير المراجعة والامتثال",
            "Documented trail of material operations and decisions for internal audit and compliance.",
            "سجل موثَّق للعمليات والقرارات الجوهرية لأغراض المراجعة الداخلية والامتثال."),
        [ReportTypeCodes.Ideas] = new(
            "Ideas Register",
            "سجل الأفكار",
            "Formal register of every submitted idea with core metadata and current status.",
            "سجل رسمي لكل فكرة مُقدَّمة يتضمن البيانات التعريفية والحالة الراهنة."),
        [ReportTypeCodes.Evaluators] = new(
            "Evaluator Performance Report",
            "تقرير أداء المُقيّمين",
            "Evaluator productivity, average awarded scores, and mean response time.",
            "إنتاجية هيئة التقييم، متوسط الدرجات المُسندة، ومتوسط زمن الاستجابة."),
        [ReportTypeCodes.Themes] = new(
            "Strategic Themes Report",
            "تقرير المسارات الاستراتيجية",
            "Performance by strategic track: submission volume, adoption rate, and realized impact.",
            "أداء كل مسار استراتيجي: حجم المقترحات، معدل الاعتماد، والأثر المُحقَّق."),
        [ReportTypeCodes.Innovators] = new(
            "Innovators Report",
            "تقرير المُبتكِرين",
            "Official roster of participating innovators, their contributions, and platform ranking.",
            "حصر رسمي للمُبتكِرين المشاركين، مساهماتهم، وترتيبهم على مستوى المنصة."),
        [ReportTypeCodes.Committee] = new(
            "Committee Decisions Report",
            "تقرير قرارات اللجنة",
            "Committee meeting minutes, formal resolutions, and quorum records.",
            "محاضر اجتماعات اللجنة، القرارات الصادرة، وسجل النِصاب."),
        [ReportTypeCodes.Trends] = new(
            "Trends & Time-Series Analysis",
            "تقرير الاتجاهات والتحليل الزمني",
            "Time-series analysis of performance: idea growth, stage progression, and monthly indicators.",
            "التحليل الزمني للأداء: نمو الأفكار، انتقال المراحل، والمؤشرات الشهرية."),
    };

    public static ReportMeta? Meta(string type) => Meta_.TryGetValue(type, out var m) ? m : null;
}
