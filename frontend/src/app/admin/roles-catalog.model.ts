export interface RoleCatalogRow {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  isSystem: boolean;
  isActive: boolean;
  sortOrder: number;
}

export interface RoleCatalogPatch {
  nameAr?: string;
  nameEn?: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  isActive?: boolean;
  sortOrder?: number;
}
