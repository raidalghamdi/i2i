import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { StagesComponent } from './stages.component';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

describe('StagesComponent', () => {
  let fixture: ComponentFixture<StagesComponent>;

  beforeEach(() => {
    const api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.resolve(null));

    TestBed.configureTestingModule({
      imports: [StagesComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: api }, provideRouter([])],
    });
    fixture = TestBed.createComponent(StagesComponent);
  });

  it('renders the hero title, the leading strategic-leadership card, and a representative stage', async () => {
    fixture.detectChanges();
    const hero = fixture.debugElement.query(By.directive(PublicPageHeroComponent))
      .componentInstance as PublicPageHeroComponent;
    await hero.load();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('The Nine Stages of Innovation');
    expect(text).toContain('Strategic leadership');
    expect(text).toContain('Idea Submission');
    expect(text).toContain('You submit your idea. It gets a reference number and enters the queue.');
    expect(text).toContain('View Ideas in This Stage');
  });
});
