export interface PostProgramIdea {
  id: string;
  code: string;
  titleAr: string;
  titleEn: string;
  status: string;
}

export interface PostProgramHistoryEntry {
  fromStage: string;
  toStage: string;
  comment: string;
  changedAt: string;
  changedById: string;
}
