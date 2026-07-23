export interface PendingApproval {
  instanceId: string;
  stepId: string;
  entityType: string;
  entityId: string;
  chainNameAr: string;
  chainNameEn: string;
  stepLabelAr: string;
  stepLabelEn: string;
  stepOrder: number;
  minApprovers: number;
  priorApprovers: number;
}

export interface ApprovalBulkResult {
  succeeded: number;
  failed: string[];
}
