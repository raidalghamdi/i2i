export interface ReportCatalogEntry {
  readonly type: string;
  readonly label: string;
}

/** The 12 report types produced by the generic report-generation endpoint (see backend ReportCatalog). */
export const REPORT_CATALOG: ReportCatalogEntry[] = [
  { type: 'executive', label: $localize`:@@reportTypeExecutive:Executive Performance Overview` },
  { type: 'detailed', label: $localize`:@@reportTypeDetailed:Comprehensive Detailed Report` },
  { type: 'media', label: $localize`:@@reportTypeMedia:Media & Corporate Communications Report` },
  { type: 'cx', label: $localize`:@@reportTypeCx:Innovator Experience Report` },
  { type: 'operational', label: $localize`:@@reportTypeOperational:Operational Performance Report` },
  { type: 'audit', label: $localize`:@@reportTypeAudit:Audit & Compliance Report` },
  { type: 'ideas', label: $localize`:@@reportTypeIdeas:Ideas Register` },
  { type: 'evaluators', label: $localize`:@@reportTypeEvaluators:Evaluator Performance Report` },
  { type: 'themes', label: $localize`:@@reportTypeThemes:Strategic Themes Report` },
  { type: 'innovators', label: $localize`:@@reportTypeInnovators:Innovators Report` },
  { type: 'committee', label: $localize`:@@reportTypeCommittee:Committee Decisions Report` },
  { type: 'trends', label: $localize`:@@reportTypeTrends:Trends & Time-Series Analysis` },
];
