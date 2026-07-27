// Change 20260726
export type EvaluationAction = 'draft' | 'submit';

// Change 20260726
export interface EvaluationCriterion {
  code: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  weight: number;
  sortOrder: number;
}

// Change 20260726
export interface EvaluationInput {
  /** Score per criterion, keyed by criterion code. Values are 0-10. */
  criteriaScores: Record<string, number>;
  comments: string | null;
  recommendation?: string | null;
  action?: EvaluationAction;
  conflictOfInterest?: boolean;
}

export interface EvaluationSubmitResult {
  id: string;
  totalScore: number;
  recommendation: string;
  ideaStatus: string;
  conflictOfInterest?: boolean; // Change 20260726
  submittedAt?: string | null; // Change 20260726
}

export interface EvaluationQueueItem {
  id: string;
  code: string;
  titleAr: string;
  titleEn: string;
  strategicThemeId: string;
  updatedAt: string;
}

export interface MyEvaluation {
  id: string;
  ideaId: string;
  ideaCode: string;
  ideaTitleEn: string;
  totalScore: number;
  recommendation: string;
  /** Null while the evaluation is still a draft. */
  submittedAt: string | null; // Change 20260726
  ideaEnteredEvaluationAt: string | null;
  criteriaScoresJson?: string; // Change 20260726
  comments?: string | null; // Change 20260726
  conflictOfInterest?: boolean; // Change 20260726
}
