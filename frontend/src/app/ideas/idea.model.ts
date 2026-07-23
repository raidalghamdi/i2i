export interface TeamMemberInput {
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

export interface Idea extends IdeaInput {
  id: string;
  code: string;
  submitterId: string;
  status: string;
  currentStage: number;
  updatedAt: string;
  attachments: IdeaAttachment[];
  screeningReason: string | null;
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
