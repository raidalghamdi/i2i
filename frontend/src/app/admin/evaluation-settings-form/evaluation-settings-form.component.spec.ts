import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { EvaluationSettingsFormComponent } from './evaluation-settings-form.component';

describe('EvaluationSettingsFormComponent', () => {
  let fixture: ComponentFixture<EvaluationSettingsFormComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EvaluationSettingsFormComponent, HttpClientTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(EvaluationSettingsFormComponent);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    httpMock.expectOne('/api/admin/evaluation/settings').flush({ passThreshold: 7.5, updatedAt: '2026-07-21T00:00:00Z' });
    await new Promise((resolve) => setTimeout(resolve));
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('loads the current threshold into the form', () => {
    expect(fixture.componentInstance.form.value.passThreshold).toBe(7.5);
  });

  it('marks the form invalid for an out-of-range value', () => {
    fixture.componentInstance.form.patchValue({ passThreshold: 11 });
    expect(fixture.componentInstance.form.invalid).toBe(true);
  });

  it('submits a PATCH with the threshold and shows a saved message', async () => {
    fixture.componentInstance.form.patchValue({ passThreshold: 7.5 });
    fixture.componentInstance.onSubmit();
    const req = httpMock.expectOne('/api/admin/evaluation/settings');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ passThreshold: 7.5 });
    req.flush({ passThreshold: 7.5, updatedAt: '2026-07-21T01:00:00Z' });
    await fixture.whenStable();
    expect(fixture.componentInstance.saved()).toBe(true);
  });

  it('shows the server error message and clears saved when the PATCH fails', async () => {
    fixture.componentInstance.form.patchValue({ passThreshold: 7.5 });
    fixture.componentInstance.onSubmit();
    const req = httpMock.expectOne('/api/admin/evaluation/settings');
    expect(req.request.method).toBe('PATCH');
    req.flush({ error: 'Passing score must be between 0 and 10.' }, { status: 400, statusText: 'Bad Request' });
    await fixture.whenStable();
    expect(fixture.componentInstance.errorMessage()).toBe('Passing score must be between 0 and 10.');
    expect(fixture.componentInstance.saved()).toBe(false);
  });
});

describe('EvaluationSettingsFormComponent (initial load failure)', () => {
  let fixture: ComponentFixture<EvaluationSettingsFormComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EvaluationSettingsFormComponent, HttpClientTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(EvaluationSettingsFormComponent);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    httpMock.expectOne('/api/admin/evaluation/settings').flush('fail', { status: 500, statusText: 'Server Error' });
    await new Promise((resolve) => setTimeout(resolve));
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('shows an error state instead of the form when the initial load fails', () => {
    expect(fixture.componentInstance.loadError()).toBeTruthy();
    expect(fixture.componentInstance.loading()).toBe(false);

    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();
  });

  it('recovers and shows the form when retry succeeds', async () => {
    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    retryButton.click();

    httpMock.expectOne('/api/admin/evaluation/settings').flush({ passThreshold: 8, updatedAt: '2026-07-22T00:00:00Z' });
    await new Promise((resolve) => setTimeout(resolve));
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.componentInstance.form.value.passThreshold).toBe(8);
    expect(fixture.nativeElement.querySelector('form')).toBeTruthy();
  });
});
