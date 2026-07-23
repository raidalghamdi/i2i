import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { PublicPageHeroComponent } from './public-page-hero.component';
import { PublicContentApiService } from '../../core/public-content-api.service';

describe('PublicPageHeroComponent', () => {
  function setup(cms: unknown) {
    const api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.resolve(cms));
    TestBed.configureTestingModule({
      imports: [PublicPageHeroComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: api }],
    });
    return TestBed.createComponent(PublicPageHeroComponent);
  }

  it('shows the default title when no CMS row exists', async () => {
    const fixture = setup(null);
    fixture.componentRef.setInput('slug', 'about');
    fixture.componentRef.setInput('defaultTitle', 'About the Program');
    fixture.detectChanges();
    await fixture.componentInstance.load();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('About the Program');
  });

  it('shows the CMS title (English locale) when a published row exists', async () => {
    const fixture = setup({ slug: 'about', titleAr: 'ع', titleEn: 'CMS About', bodyAr: 'ب', bodyEn: 'CMS body' });
    fixture.componentRef.setInput('slug', 'about');
    fixture.componentRef.setInput('defaultTitle', 'About the Program');
    fixture.detectChanges();
    await fixture.componentInstance.load();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('CMS About');
  });

  it('falls back to the default title (intentional silent fallback) when the CMS fetch fails', async () => {
    const api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.reject(new Error('boom')));
    TestBed.configureTestingModule({
      imports: [PublicPageHeroComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: api }],
    });
    const fixture = TestBed.createComponent(PublicPageHeroComponent);
    fixture.componentRef.setInput('slug', 'about');
    fixture.componentRef.setInput('defaultTitle', 'About the Program');
    fixture.detectChanges();
    await fixture.componentInstance.load();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('About the Program');
  });
});
