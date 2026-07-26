import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { TermsModalComponent } from './terms-modal.component';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicContent } from '../../core/public-content.model';

describe('TermsModalComponent', () => {
  let fixture: ComponentFixture<TermsModalComponent>;
  let api: jasmine.SpyObj<PublicContentApiService>;

  async function setup(content: PublicContent | null): Promise<void> {
    api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.resolve(content));

    await TestBed.configureTestingModule({
      imports: [TermsModalComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(TermsModalComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('fetches the "terms" CMS slug on init and renders the returned body', async () => {
    await setup({
      slug: 'terms',
      titleAr: 'الشروط والأحكام',
      titleEn: 'Terms & Conditions',
      bodyAr: 'فقرة أولى بالعربية.',
      bodyEn: 'First CMS paragraph.\n\nSecond CMS paragraph.',
    });

    expect(api.getBySlug).toHaveBeenCalledWith('terms');
    const text = (fixture.nativeElement as HTMLElement).textContent;
    expect(text).toContain('First CMS paragraph.');
    expect(text).toContain('Second CMS paragraph.');
  });

  it('renders the hardcoded fallback paragraph when there is no CMS content', async () => {
    await setup(null);

    const text = (fixture.nativeElement as HTMLElement).textContent;
    expect(text).toContain(
      'By using the Innovation to Impact platform you agree to these Terms & Conditions.',
    );
  });

  it('falls back to the hardcoded paragraphs (intentional silent fallback) when the CMS fetch fails', async () => {
    api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.reject(new Error('boom')));

    await TestBed.configureTestingModule({
      imports: [TermsModalComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(TermsModalComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent;
    expect(text).toContain(
      'By using the Innovation to Impact platform you agree to these Terms & Conditions.',
    );
  });

  it('renders as an accessible dialog with a close button that emits "closed" when clicked', async () => {
    await setup(null);

    const dialog = (fixture.nativeElement as HTMLElement).querySelector('[role="dialog"]');
    expect(dialog).toBeTruthy();
    expect(dialog?.getAttribute('aria-modal')).toBe('true');

    const emitted = jasmine.createSpy('closed');
    fixture.componentInstance.closed.subscribe(emitted);

    const closeButton = (fixture.nativeElement as HTMLElement).querySelector(
      'button[data-testid="terms-modal-close"]',
    ) as HTMLButtonElement;
    expect(closeButton).toBeTruthy();
    closeButton.click();

    expect(emitted).toHaveBeenCalled();
  });

  it('emits "closed" when clicking the backdrop, but not when clicking inside the dialog panel', async () => {
    await setup(null);

    const emitted = jasmine.createSpy('closed');
    fixture.componentInstance.closed.subscribe(emitted);

    const dialog = (fixture.nativeElement as HTMLElement).querySelector('[role="dialog"]') as HTMLElement;
    dialog.click();
    expect(emitted).not.toHaveBeenCalled();

    const backdrop = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="terms-modal-backdrop"]',
    ) as HTMLElement;
    backdrop.click();
    expect(emitted).toHaveBeenCalled();
  });

  it('emits "closed" when Escape is pressed', async () => {
    await setup(null);

    const emitted = jasmine.createSpy('closed');
    fixture.componentInstance.closed.subscribe(emitted);

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(emitted).toHaveBeenCalled();
  });
});
