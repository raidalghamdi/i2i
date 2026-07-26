export interface Escalation {
  id: string;
  entityType: string;
  entityId: string;
  tierCode: string;
  tierNameEn: string;
  reasonAr: string;
  reasonEn: string;
  statusCode: string;
  statusNameEn: string;
  ownerName: string | null;
  openedAt: string;
}

export interface EscalationDetail extends Escalation {
  resolutionAr: string | null;
  resolutionEn: string | null;
}

export interface EscalationFilter {
  status?: string;
  tier?: string;
  entityType?: string;
}

export interface EscalationActionInput {
  notes: string | null;
}

export interface EscalationResolveInput {
  resolutionAr: string;
  resolutionEn: string;
}
