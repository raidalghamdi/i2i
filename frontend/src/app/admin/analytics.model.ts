export interface PlatformKpis {
  totalIdeas: number;
  totalApproved: number;
  totalSubmitters: number;
  totalEvaluations: number;
  totalEvaluators: number;
}

export interface IdeasByStatusEntry {
  statusCode: string;
  statusNameEn: string;
  count: number;
}

export interface SubmissionsOverTimeEntry {
  date: string;
  count: number;
}

export interface ThemeActivityEntry {
  themeNameEn: string;
  ideaCount: number;
  approvedCount: number;
}

export interface TopEvaluatorEntry {
  evaluatorNameEn: string;
  evaluationCount: number;
  averageScore: number;
}

export interface SlaCompliance {
  compliancePct: number | null;
  totalTracked: number;
}

export interface AnalyticsDashboard {
  platformKpis: PlatformKpis;
  ideasByStatus: IdeasByStatusEntry[];
  submissionsOverTime: SubmissionsOverTimeEntry[];
  themeActivity: ThemeActivityEntry[];
  topEvaluators: TopEvaluatorEntry[];
  slaCompliance: SlaCompliance;
}

export interface ExtendedPlatformKpis {
  totalSubmissions: number;
  totalApproved: number;
  totalImplemented: number;
  activeSubmitters: number;
  totalEvaluations: number;
  totalUsers: number;
  totalEvaluators: number;
  realizedFinancialImpact: number;
}

export interface FunnelEntry {
  stageKey: string;
  count: number;
}

export interface CohortEntry {
  month: string;
  submitted: number;
  approved: number;
  rejected: number;
  implemented: number;
}

export interface IdeasByStageEntry {
  stage: number;
  count: number;
}

export interface TopObjectiveEntry {
  themeId: string;
  nameAr: string;
  nameEn: string;
  count: number;
}

export interface AvgTimePerStageEntry {
  stage: number;
  avgDays: number;
}

export interface ConversionResult {
  submitted: number;
  pilot: number;
  rate: number;
}

export interface ExecutiveAnalytics {
  kpis: ExtendedPlatformKpis;
  funnel: FunnelEntry[];
  cohort: CohortEntry[];
  ideasByStage: IdeasByStageEntry[];
  submissions: SubmissionsOverTimeEntry[];
  topObjectives: TopObjectiveEntry[];
  avgTimePerStage: AvgTimePerStageEntry[];
  conversion: ConversionResult;
}

export interface PillarKpis {
  ideas: number;
  budgetSpent: number;
  budgetAllocated: number;
  pilotsActive: number;
  implementationsDone: number;
}

export interface PillarTimelineEntry {
  month: string;
  count: number;
}

export interface PillarIdeaRow {
  id: string;
  code: string;
  titleAr: string;
  titleEn: string;
  status: string;
  currentStage: string;
}

export interface PillarDetail {
  themeId: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
  ownerName: string;
  kpis: PillarKpis;
  timeline: PillarTimelineEntry[];
  ideas: PillarIdeaRow[];
}
