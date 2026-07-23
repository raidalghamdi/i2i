import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { EventsComponent } from './events.component';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

describe('EventsComponent', () => {
  let fixture: ComponentFixture<EventsComponent>;

  beforeEach(() => {
    const api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.resolve(null));

    TestBed.configureTestingModule({
      imports: [EventsComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: api }, provideRouter([])],
    });
    fixture = TestBed.createComponent(EventsComponent);
  });

  it('renders the hero title and a representative card', async () => {
    fixture.detectChanges();
    const hero = fixture.debugElement.query(By.directive(PublicPageHeroComponent))
      .componentInstance as PublicPageHeroComponent;
    await hero.load();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Events');
    expect(text).toContain('Main Competition');
    expect(text).toContain('View details');
  });
});
