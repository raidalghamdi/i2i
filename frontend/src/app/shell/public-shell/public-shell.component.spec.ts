import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { PublicShellComponent } from './public-shell.component';

describe('PublicShellComponent', () => {
  let fixture: ComponentFixture<PublicShellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PublicShellComponent, HttpClientTestingModule],
      providers: [provideRouter([])],
    }).compileComponents();
    fixture = TestBed.createComponent(PublicShellComponent);
    fixture.detectChanges();
  });

  it('renders the public nav, a router-outlet and the footer without requiring identity', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('router-outlet')).toBeTruthy();
    expect(el.querySelector('app-site-footer')).toBeTruthy();
    // login CTA present for anonymous visitors
    expect(el.textContent).toContain('Log in');
  });

  it('makes the content wrapper full width (no max-width cap on the shell column)', () => {
    const el = fixture.nativeElement as HTMLElement;
    const wrapper = el.querySelector('main > div') as HTMLElement;
    expect(wrapper).toBeTruthy();
    expect(wrapper.classList.contains('max-w-5xl')).toBe(false);
    expect(wrapper.classList.contains('w-full')).toBe(true);
  });

  it('renders both the program logo and the GAC logo in the header', () => {
    const el = fixture.nativeElement as HTMLElement;
    const imgs = Array.from(el.querySelectorAll('header img')) as HTMLImageElement[];
    const srcs = imgs.map((i) => i.getAttribute('src') ?? '');
    expect(srcs.some((s) => s.includes('Competition-Innovation-Program-logo.svg'))).toBe(true);
    const gac = imgs.find((i) => (i.getAttribute('src') ?? '').includes('gac-logo-placeholder.svg'));
    expect(gac).toBeTruthy();
    expect(gac!.getAttribute('alt')).toBeTruthy();
  });
});
