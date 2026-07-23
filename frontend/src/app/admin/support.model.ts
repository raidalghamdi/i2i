export interface SupportRow {
  id: string;
  name: string;
  email: string;
  subject: string;
  body: string;
  handled: boolean;
  createdAt: string;
}

export interface SupportFilter {
  page?: number;
  pageSize?: number;
  handled?: boolean;
}

export interface SupportListResult {
  items: SupportRow[];
  total: number;
  page: number;
  pageSize: number;
}
