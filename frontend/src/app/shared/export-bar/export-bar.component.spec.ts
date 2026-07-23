import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AnalyticsApiService } from '../../admin/analytics-api.service';
import { ExportBarComponent } from './export-bar.component';

describe('ExportBarComponent', () => {
  let fixture: ComponentFixture<ExportBarComponent>;
  let analyticsApi: jasmine.SpyObj<AnalyticsApiService>;

  function setup(): void {
    analyticsApi = jasmine.createSpyObj('AnalyticsApiService', ['exportAnalytics', 'downloadReport']);

    TestBed.configureTestingModule({
      imports: [ExportBarComponent],
      providers: [{ provide: AnalyticsApiService, useValue: analyticsApi }],
    });
    fixture = TestBed.createComponent(ExportBarComponent);
  }

  function findButton(label: string): HTMLButtonElement {
    return Array.from(fixture.nativeElement.querySelectorAll('button')).find((b) =>
      (b as HTMLButtonElement).textContent?.includes(label),
    ) as HTMLButtonElement;
  }

  it('renders PDF, PPTX and XLSX buttons', () => {
    setup();
    fixture.detectChanges();

    expect(findButton('PDF')).toBeTruthy();
    expect(findButton('PPTX')).toBeTruthy();
    expect(findButton('XLSX')).toBeTruthy();
  });

  it('exports and downloads xlsx when the XLSX button is clicked', async () => {
    setup();
    analyticsApi.exportAnalytics.and.returnValue(Promise.resolve({ reportGenerationId: 'r1', status: 'completed' }));
    analyticsApi.downloadReport.and.returnValue(Promise.resolve(new Blob(['data'])));
    fixture.detectChanges();

    findButton('XLSX').click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(analyticsApi.exportAnalytics).toHaveBeenCalledWith('xlsx');
    expect(analyticsApi.downloadReport).toHaveBeenCalledWith('r1');
    expect(fixture.componentInstance.errorMessage()).toBeNull();
  });

  it('exports pdf and pptx when their respective buttons are clicked', async () => {
    setup();
    analyticsApi.exportAnalytics.and.returnValue(Promise.resolve({ reportGenerationId: 'r2', status: 'completed' }));
    analyticsApi.downloadReport.and.returnValue(Promise.resolve(new Blob(['data'])));
    fixture.detectChanges();

    findButton('PDF').click();
    await fixture.whenStable();
    expect(analyticsApi.exportAnalytics).toHaveBeenCalledWith('pdf');
    expect(analyticsApi.downloadReport).toHaveBeenCalledWith('r2');

    findButton('PPTX').click();
    await fixture.whenStable();
    expect(analyticsApi.exportAnalytics).toHaveBeenCalledWith('pptx');
  });

  it('sets the error signal and does not download when the export status is not completed', async () => {
    setup();
    analyticsApi.exportAnalytics.and.returnValue(Promise.resolve({ reportGenerationId: 'r3', status: 'failed' }));
    fixture.detectChanges();

    await fixture.componentInstance.onExportXlsx();

    expect(analyticsApi.downloadReport).not.toHaveBeenCalled();
    expect(fixture.componentInstance.errorMessage()).toBe('Export failed. Please try again.');
  });

  it('sets the error signal when the export API call throws', async () => {
    setup();
    analyticsApi.exportAnalytics.and.returnValue(Promise.reject({ error: { error: 'boom' } }));
    fixture.detectChanges();

    await fixture.componentInstance.onExportPdf();

    expect(fixture.componentInstance.errorMessage()).toBe('boom');
  });
});
