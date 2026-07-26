export interface Challenge {
  id: string;
  strategicThemeId: string;
  textAr: string;
  textEn: string;
  sortOrder: number;
  isActive: boolean;
}

export interface ChallengeInput {
  strategicThemeId: string;
  textAr: string;
  textEn: string;
  sortOrder: number;
  isActive: boolean;
}
