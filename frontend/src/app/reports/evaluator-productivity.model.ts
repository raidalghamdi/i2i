// Change 20260726
export interface EvaluatorProductivityRow {
  userId: string;
  displayName: string;
  assignedCount: number;
  completedCount: number;
  draftCount: number;
  avgScore: number | null;
  avgTurnaroundHours: number | null;
  coiCount: number;
}

export type EvaluatorProductivitySortKey = keyof Omit<EvaluatorProductivityRow, 'userId'>;
