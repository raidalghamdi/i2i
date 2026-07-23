import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { IdentityService } from '../core/auth/identity.service';
import { PlatformStatsService } from '../core/platform-stats.service';
import { PublicTracksApiService } from '../core/public-tracks-api.service';
import { LandingComponent } from './landing.component';

describe('LandingComponent', () => {
  let fixture: ComponentFixture<LandingComponent>;
  let platformStats: jasmine.SpyObj<PlatformStatsService>;
  let publicTracksApi: jasmine.SpyObj<PublicTracksApiService>;

  async function setup(activeRole: string | null): Promise<void> {
    platformStats = jasmine.createSpyObj('PlatformStatsService', ['get']);
    platformStats.get.and.returnValue(
      Promise.resolve({ totalIdeas: 5, totalApproved: 2, totalSubmitters: 3, totalEvaluations: 4, totalEvaluators: 1 }),
    );
    publicTracksApi = jasmine.createSpyObj('PublicTracksApiService', ['list']);
    publicTracksApi.list.and.returnValue(
      Promise.resolve([{ id: 'theme-1', nameAr: 'ا', nameEn: 'Theme One', descriptionAr: '', descriptionEn: '', priority: 1 }]),
    );

    TestBed.configureTestingModule({
      imports: [LandingComponent],
      providers: [
        provideRouter([]),
        { provide: PlatformStatsService, useValue: platformStats },
        { provide: PublicTracksApiService, useValue: publicTracksApi },
        {
          provide: IdentityService,
          useValue: {
            identity: signal({
              samAccountName: 'submitter1',
              email: null,
              department: null,
              roles: activeRole ? [activeRole] : [],
              activeRole,
            }),
          },
        },
      ],
    });
    fixture = TestBed.createComponent(LandingComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();
  }

  it('shows the "Submit your idea" CTA for a user with an assigned role', async () => {
    await setup('submitter');
    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href') === '/ideas/new')).toBe(true);
  });

  it('hides the "Submit your idea" CTA when there is no active role', async () => {
    await setup(null);
    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href') === '/ideas/new')).toBe(false);
  });

  it('fetches and renders platform stats for a user with a role', async () => {
    await setup('submitter');
    expect(platformStats.get).toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('5');
  });

  it('does not fetch platform stats when there is no active role', async () => {
    await setup(null);
    expect(platformStats.get).not.toHaveBeenCalled();
  });

  it('fetches and renders the public tracks regardless of role', async () => {
    await setup(null);
    expect(publicTracksApi.list).toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Theme One');
  });

  it('renders the static marketing sections (objectives, timeline, criteria, prizes, faq)', async () => {
    await setup('submitter');
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Objectives');
    expect(text).toContain('Timeline');
    expect(text).toContain('Evaluation Criteria');
    expect(text).toContain('Prizes');
    expect(text).toContain('Frequently Asked Questions');
  });

  it('renders the site footer with the copyright bar', async () => {
    await setup('submitter');
    expect(fixture.nativeElement.querySelector('footer')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('General Authority for Competition');
  });

  it('renders the public content for a genuinely anonymous visitor (identity() is null, not just role-less)', async () => {
    // Regression test: under DevAuth/Negotiate, identity() always resolves to *something* (even with
    // zero roles), so the component's original template gated its entire content behind
    // `@if (identity(); as id)`, which nobody noticed because every other test above passes a real
    // identity object. Under the JWT auth path, an anonymous visitor gets identity() === null, which
    // made the whole public landing page (hero and all) render as nothing in production.
    platformStats = jasmine.createSpyObj('PlatformStatsService', ['get']);
    publicTracksApi = jasmine.createSpyObj('PublicTracksApiService', ['list']);
    publicTracksApi.list.and.returnValue(Promise.resolve([]));

    TestBed.configureTestingModule({
      imports: [LandingComponent],
      providers: [
        provideRouter([]),
        { provide: PlatformStatsService, useValue: platformStats },
        { provide: PublicTracksApiService, useValue: publicTracksApi },
        { provide: IdentityService, useValue: { identity: signal(null) } },
      ],
    });
    fixture = TestBed.createComponent(LandingComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Objectives');
    expect(text).toContain('Timeline');
    expect(fixture.nativeElement.querySelector('footer')).not.toBeNull();
    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href') === '/ideas/new')).toBe(false);
  });
});
