export interface ScreeningQueueItem {
  id: string;
  code: string;
  titleAr: string;
  titleEn: string;
  submitterName: string;
  strategicThemeId: string;
  updatedAt: string;
}

export interface ScreeningDecisionInput {
  decisionCode: string;
  reason: string | null;
}

export interface ScreeningDecisionResult {
  id: string;
  status: string;
}

export interface TrackAssignment {
  id: string;
  evaluatorId: string;
  evaluatorName: string;
  trackId: string;
  trackNameEn: string;
}

export interface TrackAssignmentInput {
  evaluatorId: string;
  trackId: string;
}

export interface RoleUser {
  id: string;
  fullNameAr: string;
  fullNameEn: string;
}

export interface FinalRankingEntry {
  ideaId: string;
  code: string;
  titleEn: string;
  trackId: string;
  rank: number;
  score: number | null;
  outcomeStatus: string;
}

export interface FinalRankingResult {
  approvedCount: number;
  notSelectedCount: number;
  topN: number;
  entries: FinalRankingEntry[];
}
