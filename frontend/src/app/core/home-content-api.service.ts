import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { IconName } from '../shared/icon/icon.component';

/** Bilingual text pair, as returned by every text field in a home section's `contentJson`. */
export interface Loc {
  ar: string;
  en: string;
}

/** One row from `GET /api/public/home`, ordered by `idx`, visible-only. */
export interface HomeSection {
  idx: number;
  type: string;
  isVisible: boolean;
  // Nested JSON object already parsed server-side — shape depends on `type` (see the
  // per-type content interfaces below). Left as `any` here; the landing component casts
  // it via the typed accessors below for each section type it knows how to render.
  contentJson: any;
}

export interface HeroContent {
  eyebrow: Loc;
  words: Loc[];
  headline: Loc;
  subheadline: Loc;
  primaryCtaLabel: Loc;
  primaryCtaLink: string;
  secondaryCtaLabel: Loc;
  secondaryCtaLink: string;
  closedNotice: Loc;
  slogan: Loc[];
}

export interface AboutContent {
  title: Loc;
  paragraphs: Loc[];
  imageUrl?: string;
}

export interface ObjectivesContent {
  title: Loc;
  items: Loc[];
}

export interface TracksSectionContent {
  title: Loc;
  intro: Loc;
}

export interface DetailsContent {
  title: Loc;
  rulesTitle: Loc;
  rules: Loc[];
  formatTitle: Loc;
  format: Loc;
  eligibilityTitle: Loc;
  eligibility: Loc;
}

export interface SeededTimelineStage {
  id: string;
  title: Loc;
  date: Loc;
  description: Loc;
  tone: 'cyan' | 'gold';
}

export interface TimelineContent {
  title: Loc;
  stages: SeededTimelineStage[];
}

export interface SeededCriterionItem {
  label: Loc;
  description: Loc;
  weight: number;
  color: string;
  icon: IconName;
}

export interface CriteriaContent {
  title: Loc;
  eyebrow: Loc;
  lead: Loc;
  items: SeededCriterionItem[];
}

export interface SeededPrizeItem {
  tier: Loc;
  value: Loc;
}

export interface PrizesContent {
  title: Loc;
  items: SeededPrizeItem[];
}

export interface SeededGalleryItem {
  caption: Loc;
  imageUrl?: string;
}

export interface GalleryContent {
  title: Loc;
  body: Loc;
  galleryTitle: Loc;
  items: SeededGalleryItem[];
  videoTitle: Loc;
  videoHint: Loc;
  videoUrl?: string;
}

export interface PartnersContent {
  title: Loc;
  items: Loc[];
}

export interface SeededFaqItem {
  q: Loc;
  a: Loc;
}

export interface FaqContent {
  title: Loc;
  items: SeededFaqItem[];
}

export interface CtaContent {
  title: Loc;
  subtitle: Loc;
  buttonLabel: Loc;
  buttonLink: string;
}

/** Generic admin-added section (not seeded) — a title plus an HTML body per locale. */
export interface RichtextContent {
  title: Loc;
  html: Loc;
}

@Injectable({ providedIn: 'root' })
export class HomeContentApiService {
  private readonly http = inject(HttpClient);

  getHome(): Promise<HomeSection[]> {
    return firstValueFrom(this.http.get<HomeSection[]>('/api/public/home'));
  }
}
