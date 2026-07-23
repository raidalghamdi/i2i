import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AnalyticsApiService } from '../analytics-api.service';
import { AnalyticsDashboard, ExecutiveAnalytics } from '../analytics.model';
import { AnalyticsDashboardComponent } from './analytics-dashboard.component';

describe('AnalyticsDashboardComponent', () => {
  let fixture: ComponentFixture<AnalyticsDashboardComponent>;
  let analyticsApi: jasmine.SpyObj<AnalyticsApiService>;

  const sampleDashboard: AnalyticsDashboard = {
    platformKpis: { totalIdeas: 10, totalApproved: 3, totalSubmitters: 5, totalEvaluations: 8, totalEvaluators: 2 },
    ideasByStatus: [
      { statusCode: 'draft', statusNameEn: 'Draft', count: 4 },
      { statusCode: 'approved', statusNameEn: 'Approved', count: 3 },
    ],
    submissionsOverTime: [{ date: '2026-07-01T00:00:00Z', count: 2 }],
    themeActivity: [{ themeNameEn: 'Theme One', ideaCount: 5, approvedCount: 2 }],
    topEvaluators: [{ evaluatorNameEn: 'Evaluator One', evaluationCount: 4, averageScore: 7.5 }],
    slaCompliance: { compliancePct: 80, totalTracked: 5 },
  };

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
    cohort: [{ month: '2026-01-01', submitted: 10, approved: 4, rejected: 2, implemented: 1 }],
    ideasByStage: [
      { stage: 0, count: 5 },
      { stage: 1, count: 10 },
    ],
    submissions: [{ date: '2026-07-01', count: 3 }],
    topObjectives: [{ themeId: 't1', nameAr: 'موضوع', nameEn: 'Theme', count: 6 }],
    avgTimePerStage: [{ stage: 1, avgDays: 3.2 }],
    conversion: { submitted: 100, pilot: 5, rate: 5 },
  };

  function setup(
    dashboard: AnalyticsDashboard | null,
    exec: ExecutiveAnalytics | null,
    shouldThrow = false,
  ): void {
    analyticsApi = jasmine.createSpyObj('AnalyticsApiService', ['getDashboard', 'getExecutive']);
    if (shouldThrow) {
      analyticsApi.getDashboard.and.returnValue(Promise.reject({ error: { error: 'boom' } }));
      analyticsApi.getExecutive.and.returnValue(Promise.reject({ error: { error: 'boom' } }));
    } else {
      analyticsApi.getDashboard.and.returnValue(Promise.resolve(dashboard!));
      analyticsApi.getExecutive.and.returnValue(Promise.resolve(exec!));
    }

    TestBed.configureTestingModule({
      imports: [AnalyticsDashboardComponent],
      providers: [{ provide: AnalyticsApiService, useValue: analyticsApi }],
    });
    fixture = TestBed.createComponent(AnalyticsDashboardComponent);
  }

  it('renders the extended executive KPI tiles', async () => {
    setup(sampleDashboard, sampleExecutive);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('42');
    expect(text).toContain('17');
    expect(text).toContain('9');
    expect(text).toContain('25');
    expect(text).toContain('60');
    expect(text).toContain('120');
    expect(text).toContain('11');
    expect(text).toContain('250,000');
  });

  it('renders the SVG chart components with their expected inputs', async () => {
    setup(sampleDashboard, sampleExecutive);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-ideas-by-stage-chart')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('app-submissions-line-chart')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('app-cohort-chart')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('app-funnel-chart')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('app-avg-time-per-stage-table')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('app-conversion-stat-card')).toBeTruthy();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Participation');
    expect(text).toContain('Scaled');
  });

  it('renders the export bar in the header', async () => {
    setup(sampleDashboard, sampleExecutive);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-export-bar')).toBeTruthy();
  });

  it('renders theme activity and top evaluators tables from the legacy dashboard endpoint', async () => {
    setup(sampleDashboard, sampleExecutive);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Theme One');
    expect(fixture.nativeElement.textContent).toContain('Evaluator One');
  });

  it('shows "No data yet" when SLA compliance is null', async () => {
    const dashboard: AnalyticsDashboard = { ...sampleDashboard, slaCompliance: { compliancePct: null, totalTracked: 0 } };
    setup(dashboard, sampleExecutive);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No data yet');
  });

  it('shows an error message when an API call fails', async () => {
    setup(null, null, true);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBe('boom');
  });

  it('shows a spinner while loading, then an error state with retry, and recovers on retry', async () => {
    setup(null, null, true);
    fixture.detectChanges();

    expect(fixture.componentInstance.loading()).toBe(true);

    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loading()).toBe(false);
    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();

    analyticsApi.getDashboard.and.returnValue(Promise.resolve(sampleDashboard));
    analyticsApi.getExecutive.and.returnValue(Promise.resolve(sampleExecutive));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Theme One');
  });
});
