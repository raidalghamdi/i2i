export interface CommitteeCriterion {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  weight: number;
  active: boolean;
}

export interface CommitteeCriterionInput {
  code: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  weight: number;
  active: boolean;
}
