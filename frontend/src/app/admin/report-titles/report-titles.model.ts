export interface ReportTitle {
  id: string;
  key: string;
  titleAr: string;
  titleEn: string;
  sortOrder: number;
}

export interface ReportTitleInput {
  key: string;
  titleAr: string;
  titleEn: string;
  sortOrder: number;
}

export interface ReportTitlePatch {
  titleAr: string;
  titleEn: string;
  sortOrder: number;
}
