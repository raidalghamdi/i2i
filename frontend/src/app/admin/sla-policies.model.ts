// Change 20260726
export interface SlaPolicy {
  id: string;
  entityType: string;
  fromState: string;
  toState: string;
  targetHours: number;
  warnAtPct: number;
}

export interface SlaPolicyInput {
  entityType: string;
  fromState: string;
  toState: string;
  targetHours: number;
  warnAtPct: number;
}
