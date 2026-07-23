import { Component, Inject, LOCALE_ID, OnInit, computed, inject, signal } from '@angular/core';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

@Component({
  selector: 'app-terms',
  imports: [PublicPageHeroComponent],
  templateUrl: './terms.component.html',
})
export class TermsComponent implements OnInit {
  private readonly publicContent = inject(PublicContentApiService);
  private readonly isArabic: boolean;

  readonly pageTitle = $localize`:@@termsTitle:Terms & Conditions`;

  readonly today: string;

  private readonly cmsBody = signal<string | null>(null);

  readonly cmsParagraphs = computed(() => {
    const body = this.cmsBody();
    if (!body) return null;
    return body
      .split('\n\n')
      .map((paragraph) => paragraph.trim())
      .filter((paragraph) => paragraph.length > 0);
  });

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
    this.today = new Date().toLocaleDateString(this.isArabic ? 'ar-SA' : 'en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }

  async ngOnInit(): Promise<void> {
    // Intentional silent fallback: on a CMS-fetch failure this simply keeps
    // the hardcoded `paragraphs` below rather than blocking the page or
    // showing an error card over legal/terms content.
    try {
      const content = await this.publicContent.getBySlug('terms');
      if (content) {
        this.cmsBody.set(this.isArabic ? content.bodyAr : content.bodyEn);
      }
    } catch {
      // keep the hardcoded fallback paragraphs
    }
  }

  readonly paragraphs: readonly string[] = [
    $localize`:@@termsPara1:By using the Innovation to Impact platform you agree to these Terms & Conditions.`,
    $localize`:@@termsPara2:You are responsible for the accuracy of the information you submit and for ensuring you have the right to share it.`,
    $localize`:@@termsPara3:Submitted ideas are subject to the intellectual-property terms acknowledged at submission time.`,
    $localize`:@@termsPara4:The Authority may accept, request revision of, reject, or escalate any submission at its discretion based on the published evaluation criteria.`,
    $localize`:@@termsPara5:The platform is provided on an "as is" basis; the Authority may update these terms and program details from time to time.`,
  ];
}
