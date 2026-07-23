export interface CommitteeCriterion {
  code: string;
  nameAr: string;
  nameEn: string;
  weight: number;
}

export interface CommitteeDecisionInput {
  decisionTypeCode: string;
  criteriaScores: Record<string, number>;
  comments: string | null;
}

export interface CommitteeDecisionResult {
  id: string;
  totalScore: number;
  ideaStatus: string;
}

export interface CommitteeQueueItem {
  id: string;
  code: string;
  titleAr: string;
  titleEn: string;
  submitterName: string;
  hasDecided: boolean;
  decidedCount: number;
  totalJudges: number;
  updatedAt: string;
}

export interface MyCommitteeDecision {
  id: string;
  ideaId: string;
  ideaCode: string;
  ideaTitleEn: string;
  totalScore: number;
  decidedAt: string;
}
