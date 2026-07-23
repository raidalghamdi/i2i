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
});
