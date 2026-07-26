export interface EvaluationReviewItem {
  reviewerLabel: string;
  score: number | null;
  comment: string | null;
}

export interface EvaluationReviewDetail {
  evaluations: EvaluationReviewItem[];
  aggregateScore: number | null;
  aggregateByCriteria: Record<string, number> | null;
  supervisorComment: string | null;
}

export interface EvaluationReviewDecisionInput {
  decisionCode: 'forward' | 'return' | 'fail';
  supervisorComment?: string | null;
  reason?: string | null;
  editableSections?: string[] | null;
}
