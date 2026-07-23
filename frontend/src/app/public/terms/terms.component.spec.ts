import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { By } from '@angular/platform-browser';
import { TermsComponent } from './terms.component';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicContent } from '../../core/public-content.model';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

describe('TermsComponent', () => {
  let fixture: ComponentFixture<TermsComponent>;
  let api: jasmine.SpyObj<PublicContentApiService>;

  async function setup(content: PublicContent | null): Promise<void> {
    api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.resolve(content));

    await TestBed.configureTestingModule({
      imports: [TermsComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(TermsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders the hero title and the first hardcoded paragraph when there is no CMS content', async () => {
    await setup(null);

    const hero = fixture.debugElement.query(By.directive(PublicPageHeroComponent))
      .componentInstance as PublicPageHeroComponent;
    await hero.load();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Terms & Conditions');
    expect(text).toContain(
      'By using the Innovation to Impact platform you agree to these Terms & Conditions.',
    );
  });

  it('renders the CMS body (split into paragraphs) instead of the hardcoded paragraphs when CMS content is present', async () => {
    await setup({
      slug: 'terms',
      titleAr: 'الشروط والأحكام',
      titleEn: 'Terms & Conditions',
      bodyAr: 'فقرة أولى بالعربية.\n\nفقرة ثانية بالعربية.',
      bodyEn: 'First CMS paragraph.\n\nSecond CMS paragraph.',
    });

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('First CMS paragraph.');
    expect(text).toContain('Second CMS paragraph.');
    expect(text).not.toContain(
      'By using the Innovation to Impact platform you agree to these Terms & Conditions.',
    );
  });

  it('falls back to the hardcoded paragraphs (intentional silent fallback) when the CMS fetch fails', async () => {
    api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.reject(new Error('boom')));

    await TestBed.configureTestingModule({
      imports: [TermsComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(TermsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain(
      'By using the Innovation to Impact platform you agree to these Terms & Conditions.',
    );
  });
});
