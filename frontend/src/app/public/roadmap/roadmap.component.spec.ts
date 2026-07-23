import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { By } from '@angular/platform-browser';
import { RoadmapComponent } from './roadmap.component';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

describe('RoadmapComponent', () => {
  let fixture: ComponentFixture<RoadmapComponent>;

  beforeEach(() => {
    const api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.resolve(null));

    TestBed.configureTestingModule({
      imports: [RoadmapComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(RoadmapComponent);
  });

  it('renders the hero title and a representative milestone', async () => {
    fixture.detectChanges();
    const hero = fixture.debugElement.query(By.directive(PublicPageHeroComponent))
      .componentInstance as PublicPageHeroComponent;
    await hero.load();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Program Roadmap');
    expect(text).toContain('Submission Window Opens');
    expect(text).toContain('Completed');
  });

  it('renders all 6 milestones with a status pill each', () => {
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Impact Measurement');
    expect(text).toContain('In progress');
    expect(text).toContain('Upcoming');
  });
});
