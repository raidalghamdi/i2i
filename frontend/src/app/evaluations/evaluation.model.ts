export interface EvaluationInput {
  innovation: number;
  impact: number;
  execution: number;
  scalability: number;
  presentation: number;
  comments: string | null;
}

export interface EvaluationSubmitResult {
  id: string;
  totalScore: number;
  recommendation: string;
  ideaStatus: string;
}

export interface EvaluationQueueItem {
  id: string;
  code: string;
  titleAr: string;
  titleEn: string;
  submitterName: string;
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
  submittedAt: string;
  ideaEnteredEvaluationAt: string | null;
}
