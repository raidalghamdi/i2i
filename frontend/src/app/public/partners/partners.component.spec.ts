import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { By } from '@angular/platform-browser';
import { PartnersComponent } from './partners.component';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

describe('PartnersComponent', () => {
  let fixture: ComponentFixture<PartnersComponent>;

  beforeEach(() => {
    const api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.resolve(null));

    TestBed.configureTestingModule({
      imports: [PartnersComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(PartnersComponent);
  });

  it('renders the hero title and a representative partner', async () => {
    fixture.detectChanges();
    const hero = fixture.debugElement.query(By.directive(PublicPageHeroComponent))
      .componentInstance as PublicPageHeroComponent;
    await hero.load();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Our Partners');
    expect(text).toContain('SDAIA');
  });
});
