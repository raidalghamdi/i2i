import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { By } from '@angular/platform-browser';
import { TargetAudienceComponent } from './target-audience.component';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

describe('TargetAudienceComponent', () => {
  let fixture: ComponentFixture<TargetAudienceComponent>;

  beforeEach(() => {
    const api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.resolve(null));

    TestBed.configureTestingModule({
      imports: [TargetAudienceComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(TargetAudienceComponent);
  });

  it('renders the hero title and a representative card', async () => {
    fixture.detectChanges();
    const hero = fixture.debugElement.query(By.directive(PublicPageHeroComponent))
      .componentInstance as PublicPageHeroComponent;
    await hero.load();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Who Can Participate');
    expect(text).toContain('Entrepreneurs & Startups');
    expect(text).toContain(
      'Founders and emerging companies with market-ready solutions relevant to competition, market monitoring, and consumer protection.',
    );
  });
});
