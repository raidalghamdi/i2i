export interface PhaseSchedule {
  idx: number;
  code: string;
  labelAr: string;
  labelEn: string;
  startsAt: string | null;
  endsAt: string | null;
  updatedAt: string;
}

export interface PhaseScheduleUpdateInput {
  startsAt: string | null;
  endsAt: string | null;
}
