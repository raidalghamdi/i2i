import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { AboutComponent } from './about.component';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

describe('AboutComponent', () => {
  let fixture: ComponentFixture<AboutComponent>;

  beforeEach(() => {
    const api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.resolve(null));

    TestBed.configureTestingModule({
      imports: [AboutComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: api }, provideRouter([])],
    });
    fixture = TestBed.createComponent(AboutComponent);
  });

  it('renders the hero title and a representative section', async () => {
    fixture.detectChanges();
    const hero = fixture.debugElement.query(By.directive(PublicPageHeroComponent))
      .componentInstance as PublicPageHeroComponent;
    await hero.load();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('About the Program');
    expect(text).toContain('GAC employees at all levels');
    expect(text).toContain('Explore all 9 stages');
    expect(text).toContain('Contact the Innovation Office');
  });
});
