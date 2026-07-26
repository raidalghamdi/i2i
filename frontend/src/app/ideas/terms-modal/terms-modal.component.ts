import { Component, EventEmitter, HostListener, Inject, LOCALE_ID, OnInit, Output, computed, inject, signal } from '@angular/core';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { IconComponent } from '../../shared/icon/icon.component';

/** Terms & Conditions popup shown from the idea wizard. Mirrors the exact
 * fetch + fallback pattern used by the public terms page
 * (`src/app/public/terms/terms.component.ts`): fetch the `terms` CMS slug
 * on init and render its bilingual body, silently keeping the hardcoded
 * fallback paragraphs below on any fetch failure or empty response. */
@Component({
  selector: 'app-terms-modal',
  imports: [IconComponent],
  templateUrl: './terms-modal.component.html',
})
export class TermsModalComponent implements OnInit {
  private readonly publicContent = inject(PublicContentApiService);
  private readonly isArabic: boolean;

  @Output() readonly closed = new EventEmitter<void>();

  readonly modalTitle = $localize`:@@termsTitle:Terms & Conditions`;
  readonly closeLabel = $localize`:@@termsModalClose:Close`;

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
  }

  async ngOnInit(): Promise<void> {
    // Intentional silent fallback: on a CMS-fetch failure this simply keeps
    // the hardcoded `paragraphs` below rather than blocking the modal or
    // showing an error over legal/terms content.
    try {
      const content = await this.publicContent.getBySlug('terms');
      if (content) {
        this.cmsBody.set(this.isArabic ? content.bodyAr : content.bodyEn);
      }
    } catch {
      // keep the hardcoded fallback paragraphs
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closed.emit();
  }

  onBackdropClick(): void {
    this.closed.emit();
  }

  onClose(): void {
    this.closed.emit();
  }

  readonly paragraphs: readonly string[] = [
    $localize`:@@termsPara1:By using the Innovation to Impact platform you agree to these Terms & Conditions.`,
    $localize`:@@termsPara2:You are responsible for the accuracy of the information you submit and for ensuring you have the right to share it.`,
    $localize`:@@termsPara3:Submitted ideas are subject to the intellectual-property terms acknowledged at submission time.`,
    $localize`:@@termsPara4:The Authority may accept, request revision of, reject, or escalate any submission at its discretion based on the published evaluation criteria.`,
    $localize`:@@termsPara5:The platform is provided on an "as is" basis; the Authority may update these terms and program details from time to time.`,
  ];
}
