// Change 20260726
export interface EvaluationCriterion {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  weight: number;
  active: boolean;
  sortOrder: number;
}

export interface EvaluationCriterionInput {
  code: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  weight: number;
  active: boolean;
  sortOrder: number;
}
