import { Component, Inject, LOCALE_ID } from '@angular/core';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

@Component({
  selector: 'app-privacy',
  imports: [PublicPageHeroComponent],
  templateUrl: './privacy.component.html',
})
export class PrivacyComponent {
  readonly pageTitle = $localize`:@@privacyTitle:Privacy Policy`;

  readonly today: string;

  constructor(@Inject(LOCALE_ID) locale: string) {
    const isArabic = locale.startsWith('ar');
    this.today = new Date().toLocaleDateString(isArabic ? 'ar-SA' : 'en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }

  readonly paragraphs: readonly string[] = [
    $localize`:@@privacyPara1:This Privacy Policy explains how the General Authority for Competition collects, uses, and protects personal information submitted through the Innovation to Impact platform.`,
    $localize`:@@privacyPara2:We collect only the information necessary to receive, evaluate, and act on submitted ideas, including your name, contact details, and the content of your submission.`,
    $localize`:@@privacyPara3:Your information is accessible only to authorized personnel involved in evaluation and program administration, and is never sold or shared for marketing purposes.`,
    $localize`:@@privacyPara4:We retain submission data for as long as necessary to fulfill the purposes described here and to comply with applicable regulations.`,
    $localize`:@@privacyPara5:You may request access to or correction of your personal information by contacting the innovation team through the Support page.`,
  ];
}
