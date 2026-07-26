export interface PhaseSchedule {
  idx: number;
  code: string;
  labelAr: string;
  labelEn: string;
  startsAt: string | null;
  endsAt: string | null;
  updatedAt: string;
  announcedAt?: string | null;
}

export interface PhaseScheduleUpdateInput {
  startsAt: string | null;
  endsAt: string | null;
}

export interface PhaseAudience {
  roleCodes: string[];
}

export interface PhaseAnnounceResult {
  recipientCount: number;
}
