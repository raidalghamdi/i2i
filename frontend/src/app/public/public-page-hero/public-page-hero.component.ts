import { Component, Inject, LOCALE_ID, OnInit, computed, inject, input, signal } from '@angular/core';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicContent } from '../../core/public-content.model';

@Component({
  selector: 'app-public-page-hero',
  templateUrl: './public-page-hero.component.html',
})
export class PublicPageHeroComponent implements OnInit {
  private readonly api = inject(PublicContentApiService);
  private readonly isArabic: boolean;

  readonly slug = input.required<string>();
  readonly defaultTitle = input.required<string>();
  readonly defaultBody = input('');

  private readonly cms = signal<PublicContent | null>(null);

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  readonly title = computed(() => {
    const c = this.cms();
    if (!c) return this.defaultTitle();
    return this.isArabic ? c.titleAr : c.titleEn;
  });

  readonly body = computed(() => {
    const c = this.cms();
    if (!c) return this.defaultBody();
    return this.isArabic ? c.bodyAr : c.bodyEn;
  });

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async load(): Promise<void> {
    // Intentional silent fallback: the hero always has a default title/body
    // supplied by its caller, so a CMS-fetch failure should just leave the
    // defaults on screen rather than block the page or show an error card.
    try {
      this.cms.set(await this.api.getBySlug(this.slug()));
    } catch {
      // keep whatever default title/body the caller passed in
    }
  }
}
