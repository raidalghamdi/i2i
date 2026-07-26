export interface CmsBlock {
  id: string;
  key: string;
  contentAr: string;
  contentEn: string;
  isPublished: boolean;
  updatedAt: string;
}

export interface CmsBlockInput {
  key: string;
  contentAr: string;
  contentEn: string;
  isPublished: boolean;
}

export interface CmsContent {
  id: string;
  slug: string;
  titleAr: string;
  titleEn: string;
  bodyAr: string;
  bodyEn: string;
  isPublished: boolean;
  publishedAt: string | null;
  updatedAt: string;
}

export interface CmsContentInput {
  slug: string;
  titleAr: string;
  titleEn: string;
  bodyAr: string;
  bodyEn: string;
  isPublished: boolean;
}

export interface ContentString {
  id: string;
  key: string;
  valueAr: string;
  valueEn: string;
  updatedAt: string;
}

export interface ContentStringInput {
  key: string;
  valueAr: string;
  valueEn: string;
}
