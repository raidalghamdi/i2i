import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { By } from '@angular/platform-browser';
import { FaqComponent } from './faq.component';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

describe('FaqComponent', () => {
  let fixture: ComponentFixture<FaqComponent>;

  beforeEach(() => {
    const api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.resolve(null));

    TestBed.configureTestingModule({
      imports: [FaqComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(FaqComponent);
  });

  it('renders the hero title and a representative question', async () => {
    fixture.detectChanges();
    const hero = fixture.debugElement.query(By.directive(PublicPageHeroComponent))
      .componentInstance as PublicPageHeroComponent;
    await hero.load();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Frequently Asked Questions');
    expect(text).toContain('Who can submit an idea?');
    expect(text).toContain(
      'Employees of the Authority, entrepreneurs, government entities, and innovation specialists are all welcome to submit.',
    );
  });

  it('renders all 10 questions as accordion entries', () => {
    fixture.detectChanges();
    const items = fixture.nativeElement.querySelectorAll('details');
    expect(items.length).toBe(10);
  });
});
