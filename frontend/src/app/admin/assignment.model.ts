export interface Assignment {
  id: string;
  ideaId: string;
  ideaCode: string;
  ideaTitleAr: string;
  ideaTitleEn: string;
  evaluatorId: string;
  evaluatorName: string;
  assignedAt: string;
  dueAt: string | null;
  statusCode: string;
  notes: string | null;
}

export interface AssignmentPage {
  items: Assignment[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AssignmentListFilter {
  evaluatorId?: string;
  status?: string;
  ideaSearch?: string;
  page: number;
  pageSize: number;
}

export interface WorkloadRow {
  evaluatorId: string;
  evaluatorName: string;
  pending: number;
  dueSoon: number;
  overdue: number;
  completedRecent: number;
}

export interface SuggestedEvaluator {
  evaluatorId: string;
  evaluatorName: string;
  openCount: number;
}

export interface IdeaOption {
  id: string;
  code: string;
  titleAr: string;
  titleEn: string;
}

export interface AssignmentCreateInput {
  ideaId: string;
  evaluatorId: string;
  dueAt: string | null;
  notes: string | null;
}

export interface AssignmentUpdateInput {
  statusCode: string;
  dueAt: string | null;
  notes: string | null;
  evaluatorId: string;
}

export interface AssignmentUpdateResult {
  id: string;
  statusCode: string;
  evaluatorId: string;
  dueAt: string | null;
  notes: string | null;
}

export interface BulkAssignmentCreateResultItem {
  status: string;
  id: string | null;
}

export interface BulkAssignmentCreateResult {
  created: BulkAssignmentCreateResultItem[];
}
