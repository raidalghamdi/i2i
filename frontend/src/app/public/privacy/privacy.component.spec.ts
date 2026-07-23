import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { By } from '@angular/platform-browser';
import { PrivacyComponent } from './privacy.component';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

describe('PrivacyComponent', () => {
  let fixture: ComponentFixture<PrivacyComponent>;

  beforeEach(() => {
    const api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.resolve(null));

    TestBed.configureTestingModule({
      imports: [PrivacyComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(PrivacyComponent);
  });

  it('renders the hero title and the first paragraph', async () => {
    fixture.detectChanges();
    const hero = fixture.debugElement.query(By.directive(PublicPageHeroComponent))
      .componentInstance as PublicPageHeroComponent;
    await hero.load();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Privacy Policy');
    expect(text).toContain(
      'This Privacy Policy explains how the General Authority for Competition collects, uses, and protects personal information submitted through the Innovation to Impact platform.',
    );
  });
});
