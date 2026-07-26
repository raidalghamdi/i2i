import { Component, Inject, LOCALE_ID, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IdentityService } from '../core/auth/identity.service';
import { RoleCodes } from '../core/auth/role-codes';
import { IconComponent } from '../shared/icon/icon.component';
import { PublicTracksApiService } from '../core/public-tracks-api.service';
import { PublicTrack } from '../core/public-data.model';
import {
  AboutContent,
  CriteriaContent,
  CtaContent,
  DetailsContent,
  FaqContent,
  GalleryContent,
  HeroContent,
  HomeContentApiService,
  HomeSection,
  Loc,
  ObjectivesContent,
  PartnersContent,
  PrizesContent,
  RichtextContent,
  SeededGalleryItem,
  TimelineContent,
  TracksSectionContent,
} from '../core/home-content-api.service';
import { HeroRotatorComponent } from './hero-rotator/hero-rotator.component';
import { HeroNetworkComponent } from './hero-network/hero-network.component';
import { TimelineModernComponent, TimelineStage } from './timeline-modern/timeline-modern.component';
import { StickyCtaComponent } from './sticky-cta/sticky-cta.component';

const IDEA_AUTHOR_ROLES: readonly string[] = [RoleCodes.Submitter, RoleCodes.Admin];

@Component({
  selector: 'app-landing',
  imports: [RouterLink, IconComponent, HeroRotatorComponent, HeroNetworkComponent, TimelineModernComponent, StickyCtaComponent],
  templateUrl: './landing.component.html',
})
export class LandingComponent implements OnInit {
  private readonly identityService = inject(IdentityService);
  private readonly publicTracksApiService = inject(PublicTracksApiService);
  private readonly homeContentApiService = inject(HomeContentApiService);
  private readonly isArabic: boolean;
  readonly identity = this.identityService.identity;

  readonly canSubmitIdea = computed(() => {
    const activeRole = this.identity()?.activeRole;
    return !!activeRole && IDEA_AUTHOR_ROLES.includes(activeRole);
  });

  readonly tracks = signal<PublicTrack[]>([]);

  // Sections rendered by the landing page, sourced from the admin-editable, seeded
  // `GET /api/public/home` endpoint. This is the single source of truth for landing content —
  // there is intentionally no hardcoded fallback array. If the fetch fails, `sections` stays
  // empty and the page simply renders its header/footer chrome with no dynamic sections.
  readonly sections = signal<HomeSection[]>([]);

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  trackName(track: PublicTrack): string {
    return this.isArabic ? track.nameAr : track.nameEn;
  }

  trackDescription(track: PublicTrack): string {
    return this.isArabic ? track.descriptionAr : track.descriptionEn;
  }

  async ngOnInit(): Promise<void> {
    try {
      this.tracks.set(await this.publicTracksApiService.list());
    } catch {
      // The tracks section simply renders empty if the public tracks endpoint is unreachable.
    }
    try {
      this.sections.set(await this.homeContentApiService.getHome());
    } catch {
      // See the `sections` doc comment above — no hardcoded fallback content.
    }
  }

  /** Bilingual text pair -> the string for the current locale. */
  loc(pair: Loc | null | undefined): string {
    if (!pair) return '';
    return this.isArabic ? pair.ar : pair.en;
  }

  hero(section: HomeSection): HeroContent {
    return section.contentJson as HeroContent;
  }

  heroWords(section: HomeSection): string[] {
    return this.hero(section).words.map((word) => this.loc(word));
  }

  about(section: HomeSection): AboutContent {
    return section.contentJson as AboutContent;
  }

  objectives(section: HomeSection): ObjectivesContent {
    return section.contentJson as ObjectivesContent;
  }

  tracksSection(section: HomeSection): TracksSectionContent {
    return section.contentJson as TracksSectionContent;
  }

  details(section: HomeSection): DetailsContent {
    return section.contentJson as DetailsContent;
  }

  timeline(section: HomeSection): TimelineContent {
    return section.contentJson as TimelineContent;
  }

  timelineStages(section: HomeSection): TimelineStage[] {
    return this.timeline(section).stages.map((stage) => ({
      id: stage.id,
      title: this.loc(stage.title),
      date: this.loc(stage.date),
      description: this.loc(stage.description),
      tone: stage.tone,
    }));
  }

  criteria(section: HomeSection): CriteriaContent {
    return section.contentJson as CriteriaContent;
  }

  prizes(section: HomeSection): PrizesContent {
    return section.contentJson as PrizesContent;
  }

  gallery(section: HomeSection): GalleryContent {
    return section.contentJson as GalleryContent;
  }

  /** Gallery items pre-date the `imageUrl`/`caption` shape — old seeded rows are plain `Loc`
   * objects (`{ar, en}`), not `{caption, imageUrl}`. These accessors stay safe at runtime for
   * either shape rather than assuming every stored `contentJson` has already been migrated. */
  galleryItemCaption(item: SeededGalleryItem | Loc): Loc {
    return (item as SeededGalleryItem)?.caption ?? (item as Loc);
  }

  galleryItemImageUrl(item: SeededGalleryItem | Loc): string {
    return (item as SeededGalleryItem)?.imageUrl ?? '';
  }

  partners(section: HomeSection): PartnersContent {
    return section.contentJson as PartnersContent;
  }

  faq(section: HomeSection): FaqContent {
    return section.contentJson as FaqContent;
  }

  cta(section: HomeSection): CtaContent {
    return section.contentJson as CtaContent;
  }

  richtext(section: HomeSection): RichtextContent {
    return section.contentJson as RichtextContent;
  }
}
