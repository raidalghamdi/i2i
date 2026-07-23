import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ReportsApiService } from '../reports-api.service';
import { REPORT_CATALOG } from './report-catalog';
import { ReportsDashboardComponent } from './reports-dashboard.component';

describe('ReportsDashboardComponent', () => {
  let fixture: ComponentFixture<ReportsDashboardComponent>;
  let reportsApi: jasmine.SpyObj<ReportsApiService>;

  function setup(): void {
    reportsApi = jasmine.createSpyObj('ReportsApiService', ['generateReport', 'exportAnalytics', 'downloadReport']);

    TestBed.configureTestingModule({
      imports: [ReportsDashboardComponent],
      providers: [{ provide: ReportsApiService, useValue: reportsApi }],
    });
    fixture = TestBed.createComponent(ReportsDashboardComponent);
  }

  it('renders a generate control for all 12 catalog report types', () => {
    setup();
    fixture.detectChanges();

    expect(REPORT_CATALOG.length).toBe(12);
    const buttons = fixture.debugElement.queryAll(By.css('[data-testid="report-generate"]'));
    expect(buttons.length).toBe(12);
  });

  it('renders an analytics export control', () => {
    setup();
    fixture.detectChanges();

    const button = fixture.debugElement.query(By.css('[data-testid="analytics-export"]'));
    expect(button).toBeTruthy();
  });

  it('generates and downloads a report on success', async () => {
    setup();
    fixture.detectChanges();
    reportsApi.generateReport.and.returnValue(
      Promise.resolve({ reportGenerationId: 'r1', status: 'completed', fileUrl: '/tmp/x.xlsx' }),
    );
    reportsApi.downloadReport.and.returnValue(Promise.resolve(new Blob(['data'])));

    await fixture.componentInstance.onGenerate('executive');

    expect(reportsApi.generateReport).toHaveBeenCalledWith('executive', { format: 'xlsx' });
    expect(reportsApi.downloadReport).toHaveBeenCalledWith('r1');
    expect(fixture.componentInstance.errorMessage()).toBeNull();
  });

  it('passes the selected format to generateReport and uses it as the download extension', async () => {
    setup();
    fixture.detectChanges();
    reportsApi.generateReport.and.returnValue(
      Promise.resolve({ reportGenerationId: 'r9', status: 'completed', fileUrl: '/tmp/x.pdf' }),
    );
    reportsApi.downloadReport.and.returnValue(Promise.resolve(new Blob(['data'])));
    fixture.componentInstance.format.set('pdf');

    await fixture.componentInstance.onGenerate('executive');

    expect(reportsApi.generateReport).toHaveBeenCalledWith('executive', { format: 'pdf' });
    expect(reportsApi.downloadReport).toHaveBeenCalledWith('r9');
  });

  it('shows an error and does not download when generation status is failed', async () => {
    setup();
    fixture.detectChanges();
    reportsApi.generateReport.and.returnValue(Promise.resolve({ reportGenerationId: 'r2', status: 'failed', fileUrl: null }));

    await fixture.componentInstance.onGenerate('ideas');

    expect(reportsApi.downloadReport).not.toHaveBeenCalled();
    expect(fixture.componentInstance.errorMessage()).toBe('Report generation failed. Please try again.');
  });

  it('generates and downloads a different report type on success', async () => {
    setup();
    fixture.detectChanges();
    reportsApi.generateReport.and.returnValue(
      Promise.resolve({ reportGenerationId: 'r3', status: 'completed', fileUrl: '/tmp/y.xlsx' }),
    );
    reportsApi.downloadReport.and.returnValue(Promise.resolve(new Blob(['data'])));

    await fixture.componentInstance.onGenerate('trends');

    expect(reportsApi.generateReport).toHaveBeenCalledWith('trends', { format: 'xlsx' });
    expect(reportsApi.downloadReport).toHaveBeenCalledWith('r3');
  });

  it('exports analytics and downloads it on success', async () => {
    setup();
    fixture.detectChanges();
    reportsApi.exportAnalytics.and.returnValue(
      Promise.resolve({ reportGenerationId: 'r4', status: 'completed', fileUrl: '/tmp/z.xlsx' }),
    );
    reportsApi.downloadReport.and.returnValue(Promise.resolve(new Blob(['data'])));

    await fixture.componentInstance.onExportAnalytics();

    expect(reportsApi.exportAnalytics).toHaveBeenCalledWith('xlsx');
    expect(reportsApi.downloadReport).toHaveBeenCalledWith('r4');
  });

  it('shows a generic error message when the generate API call throws', async () => {
    setup();
    fixture.detectChanges();
    reportsApi.generateReport.and.returnValue(Promise.reject({ error: { error: 'boom' } }));

    await fixture.componentInstance.onGenerate('executive');

    expect(fixture.componentInstance.errorMessage()).toBe('boom');
  });
});
