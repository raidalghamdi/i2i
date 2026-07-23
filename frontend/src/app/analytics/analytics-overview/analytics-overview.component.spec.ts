import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AnalyticsApiService } from '../../admin/analytics-api.service';
import { ExecutiveAnalytics } from '../../admin/analytics.model';
import { AnalyticsOverviewComponent } from './analytics-overview.component';

const sampleExecutive: ExecutiveAnalytics = {
  kpis: {
    totalSubmissions: 42,
    totalApproved: 17,
    totalImplemented: 9,
    activeSubmitters: 25,
    totalEvaluations: 60,
    totalUsers: 120,
    totalEvaluators: 11,
    realizedFinancialImpact: 250000,
  },
  funnel: [
    { stageKey: 'Participation', count: 100 },
    { stageKey: 'Evaluated', count: 60 },
    { stageKey: 'Approved', count: 20 },
    { stageKey: 'Piloted', count: 5 },
    { stageKey: 'Scaled', count: 1 },
  ],
  cohort: [],
  ideasByStage: [],
  submissions: [],
  topObjectives: [
    { themeId: 'theme-1', nameAr: 'موضوع واحد', nameEn: 'Theme One', count: 12 },
    { themeId: 'theme-2', nameAr: 'موضوع اثنان', nameEn: 'Theme Two', count: 8 },
  ],
  avgTimePerStage: [],
  conversion: { submitted: 100, pilot: 20, rate: 20 },
};

class StubAnalyticsApi {
  getExecutive() {
    return Promise.resolve(sampleExecutive);
  }
}

describe('AnalyticsOverviewComponent', () => {
  let fixture: ComponentFixture<AnalyticsOverviewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AnalyticsOverviewComponent],
      providers: [
        provideRouter([]),
        { provide: AnalyticsApiService, useClass: StubAnalyticsApi },
        { provide: LOCALE_ID, useValue: 'en' },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AnalyticsOverviewComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('renders the 5 executive KPI tiles', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('42');
    expect(text).toContain('17');
    expect(text).toContain('9');
    expect(text).toContain('20');
    expect(text).toContain('250,000');
  });

  it('renders the conversion funnel chart with localized stage labels', () => {
    expect(fixture.nativeElement.querySelector('app-funnel-chart')).toBeTruthy();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Participation');
    expect(text).toContain('Scaled');
  });

  it('links each top objective to its pillar drill-down page', () => {
    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href')?.includes('/analytics/pillars/theme-1'))).toBeTrue();
    expect(links.some((a) => a.getAttribute('href')?.includes('/analytics/pillars/theme-2'))).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('Theme One');
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    const api = { getExecutive: jasmine.createSpy('getExecutive') };
    api.getExecutive.and.returnValue(Promise.reject({ error: { error: 'Analytics unavailable' } }));

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [AnalyticsOverviewComponent],
      providers: [
        provideRouter([]),
        { provide: AnalyticsApiService, useValue: api },
        { provide: LOCALE_ID, useValue: 'en' },
      ],
    });
    const failFixture = TestBed.createComponent(AnalyticsOverviewComponent);
    failFixture.detectChanges();
    await failFixture.whenStable();
    failFixture.detectChanges();

    expect(failFixture.componentInstance.loadError()).toBe('Analytics unavailable');
    const retryButton = failFixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    api.getExecutive.and.returnValue(Promise.resolve(sampleExecutive));
    retryButton.click();
    await failFixture.whenStable();
    failFixture.detectChanges();

    expect(failFixture.componentInstance.loadError()).toBeNull();
    expect(failFixture.nativeElement.textContent).toContain('42');
  });
});
