import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { LOCALE_ID } from '@angular/core';
import { By } from '@angular/platform-browser';
import { PilotsComponent } from './pilots.component';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

describe('PilotsComponent', () => {
  let fixture: ComponentFixture<PilotsComponent>;

  beforeEach(() => {
    const api = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    api.getBySlug.and.returnValue(Promise.resolve(null));

    TestBed.configureTestingModule({
      imports: [PilotsComponent, HttpClientTestingModule],
      providers: [
        { provide: PublicContentApiService, useValue: api },
        { provide: LOCALE_ID, useValue: 'en' },
      ],
    });
    fixture = TestBed.createComponent(PilotsComponent);
  });

  it('renders the hero title and a representative pilot card', async () => {
    fixture.detectChanges();
    const hero = fixture.debugElement.query(By.directive(PublicPageHeroComponent))
      .componentInstance as PublicPageHeroComponent;
    await hero.load();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Pilot & experiment tracker');
    expect(text).toContain('Retailer complaints portal');
    expect(text).toContain('A guided portal increases SME complaints by 30%.');
    expect(text).toContain('150000 SAR');
    expect(text).toContain('Complaints up 24% at mid-review.');
    expect(text).toContain('Launch');
    expect(text).toContain('Running');
  });
});
