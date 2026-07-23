export interface EmailLogRow {
  id: string;
  provider: string;
  statusCode: string;
  statusNameAr: string;
  statusNameEn: string;
  providerMessageId: string | null;
  redirectApplied: boolean;
  toEmail: string;
  sentAt: string;
}

export interface EmailLogFilter {
  page?: number;
  pageSize?: number;
  status?: string;
}

export interface EmailLogListResult {
  items: EmailLogRow[];
  total: number;
  page: number;
  pageSize: number;
}
