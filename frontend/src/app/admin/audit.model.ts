export interface AuditRow {
  id: string;
  chainSeq: number;
  occurredAt: string;
  actorName: string | null;
  entityType: string;
  entityId: string;
  action: string;
  verified: boolean;
}

export interface AuditFilter {
  entityType?: string;
  action?: string;
  actorId?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export interface AuditBrowseResult {
  items: AuditRow[];
  total: number;
  page: number;
  pageSize: number;
}
