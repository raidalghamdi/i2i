export interface TeamMemberInput {
  samAccountName: string;
  name: string;
  email: string;
}

export interface IdeaInput {
  titleAr: string;
  titleEn: string;
  problemStatementAr: string;
  problemStatementEn: string;
  proposedSolutionAr: string;
  proposedSolutionEn: string;
  expectedBenefitsAr: string;
  expectedBenefitsEn: string;
  strategicThemeId: string;
  activityId: string;
  challengeId: string | null;
  participationType: 'individual' | 'team';
  teamName: string | null;
  teamMembers: TeamMemberInput[];
  ipAcknowledged: boolean;
  termsAgreed: boolean;
}

export interface IdeaSummary {
  id: string;
  code: string;
  titleAr: string;
  titleEn: string;
  status: string;
  updatedAt: string;
}

export interface IdeaAttachment {
  id: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedAt: string;
}

/** The submitter's identity (idea's team lead), surfaced by the detail endpoint. */
export interface IdeaSubmitter {
  name: string;
  samAccountName: string | null;
  email: string | null;
}

/** Payload for POST /api/ideas/:id/resubmit — the fields a returned idea can change. */
export interface IdeaResubmitInput {
  titleAr: string;
  titleEn: string;
  problemStatementAr: string;
  problemStatementEn: string;
  proposedSolutionAr: string;
  proposedSolutionEn: string;
  expectedBenefitsAr: string;
  expectedBenefitsEn: string;
  activityId: string;
  strategicThemeId: string;
  challengeId: string | null;
  participationType: 'individual' | 'team';
  teamName: string | null;
  teamMembers: TeamMemberInput[];
}

export interface Idea extends IdeaInput {
  id: string;
  code: string;
  submitterId: string | null;
  submitter?: IdeaSubmitter | null;
  status: string;
  currentStage: number;
  createdAt: string;
  updatedAt: string;
  approvedAt: string | null;
  attachments: IdeaAttachment[];
  screeningReason: string | null;
  /** Comma-joined list of section keys the submitter may edit while `returned` (or `null`). */
  editableSections: string | null;
}

export interface StrategicTheme {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
}

export interface Activity {
  id: string;
  nameAr: string;
  nameEn: string;
}

export interface Challenge {
  id: string;
  textAr: string;
  textEn: string;
}

export type StageState = 'completed' | 'current' | 'stopped' | 'upcoming';

export interface JourneyStage {
  index: number;
  state: StageState;
  label: { ar: string; en: string };
  completedAt: string | null;
}

export interface IdeaJourney {
  currentStage: number;
  stopped: boolean;
  evaluationScore: number | null;
  stages: JourneyStage[];
}

export interface IdeaListItem {
  id: string;
  code: string;
  titleAr: string;
  titleEn: string;
  problemStatementAr: string;
  problemStatementEn: string;
  currentStage: number;
  status: string;
  strategicThemeId: string;
  activityId: string | null;
}

export interface IdeaListPage {
  items: IdeaListItem[];
  total: number;
  page: number;
  pageSize: number;
}

export interface MyIdeaItem {
  id: string;
  code: string;
  titleAr: string;
  titleEn: string;
  status: string;
  currentStage: number;
  createdAt: string;
  updatedAt: string;
  feedbackCount: number;
  isOwner: boolean;
}

export interface IdeaListFilters {
  q?: string;
  strategicThemeId?: string;
  activityId?: string;
  status?: string;
  stage?: number;
  page?: number;
  pageSize?: number;
}
