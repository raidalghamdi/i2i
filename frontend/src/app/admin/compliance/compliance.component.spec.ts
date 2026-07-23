import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { provideRouter } from '@angular/router';
import { ComplianceApiService } from '../compliance-api.service';
import { ComplianceControlRow } from '../compliance.model';
import { ComplianceComponent } from './compliance.component';

describe('ComplianceComponent', () => {
  let fixture: ComponentFixture<ComplianceComponent>;
  let complianceApi: jasmine.SpyObj<ComplianceApiService>;

  const metRow: ComplianceControlRow = {
    id: 'c1',
    controlCode: 'ISO-27001-A.5.1',
    standardBodyCode: 'iso27001',
    standardBodyNameAr: 'الأيزو 27001',
    standardBodyNameEn: 'ISO 27001',
    titleAr: 'سياسات أمن المعلومات',
    titleEn: 'Information security policies',
    descriptionAr: 'وصف',
    descriptionEn: 'Description',
    statusCode: 'met',
    statusNameAr: 'مستوفى',
    statusNameEn: 'Met',
    mappedFeaturePaths: ['/admin/users', '/admin/roster'],
  };

  const notStartedRow: ComplianceControlRow = {
    id: 'c2',
    controlCode: 'NCA-ECC-2.1',
    standardBodyCode: 'nca-ecc',
    standardBodyNameAr: 'الهيئة الوطنية للأمن السيبراني',
    standardBodyNameEn: 'NCA ECC',
    titleAr: 'إدارة الهوية والوصول',
    titleEn: 'Identity and access management',
    descriptionAr: 'وصف',
    descriptionEn: 'Description',
    statusCode: 'not_started',
    statusNameAr: 'لم يبدأ',
    statusNameEn: 'Not started',
    mappedFeaturePaths: [],
  };

  function setup(rows: ComplianceControlRow[]): void {
    complianceApi = jasmine.createSpyObj('ComplianceApiService', ['list']);
    complianceApi.list.and.returnValue(Promise.resolve(rows));

    TestBed.configureTestingModule({
      imports: [ComplianceComponent],
      providers: [
        provideRouter([]),
        { provide: ComplianceApiService, useValue: complianceApi },
        { provide: LOCALE_ID, useValue: 'en' },
      ],
    });
    fixture = TestBed.createComponent(ComplianceComponent);
  }

  async function init(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders one row per compliance control with localized standard, requirement and status', async () => {
    setup([metRow, notStartedRow]);
    await init();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('ISO-27001-A.5.1');
    expect(text).toContain('ISO 27001');
    expect(text).toContain('Information security policies');
    expect(text).toContain('Met');
    expect(text).toContain('NCA-ECC-2.1');
    expect(text).toContain('NCA ECC');
    expect(text).toContain('Identity and access management');
    expect(text).toContain('Not started');
  });

  it('shows a chip for each mapped feature path', async () => {
    setup([metRow, notStartedRow]);
    await init();

    const chips = fixture.nativeElement.querySelectorAll('code');
    expect(chips.length).toBe(2);
    expect(chips[0].textContent).toContain('/admin/users');
    expect(chips[1].textContent).toContain('/admin/roster');
  });

  it('shows an em dash when a control has no mapped feature paths', async () => {
    setup([notStartedRow]);
    await init();

    const cells = fixture.nativeElement.querySelectorAll('td');
    const lastCellText = cells[cells.length - 1].textContent.trim();
    expect(lastCellText).toBe('—');
  });

  it('shows an empty-state message when there are no compliance controls', async () => {
    setup([]);
    await init();

    expect(fixture.nativeElement.textContent).toContain('No compliance controls');
  });

  it('sets an error message when the list call fails', async () => {
    complianceApi = jasmine.createSpyObj('ComplianceApiService', ['list']);
    complianceApi.list.and.returnValue(Promise.reject(new Error('boom')));

    TestBed.configureTestingModule({
      imports: [ComplianceComponent],
      providers: [
        provideRouter([]),
        { provide: ComplianceApiService, useValue: complianceApi },
        { provide: LOCALE_ID, useValue: 'en' },
      ],
    });
    fixture = TestBed.createComponent(ComplianceComponent);
    await init();

    expect(fixture.componentInstance.errorMessage()).toBeTruthy();
  });

  it('shows an error state with retry when the list call fails, and recovers on retry', async () => {
    complianceApi = jasmine.createSpyObj('ComplianceApiService', ['list']);
    complianceApi.list.and.returnValue(Promise.reject(new Error('boom')));

    TestBed.configureTestingModule({
      imports: [ComplianceComponent],
      providers: [
        provideRouter([]),
        { provide: ComplianceApiService, useValue: complianceApi },
        { provide: LOCALE_ID, useValue: 'en' },
      ],
    });
    fixture = TestBed.createComponent(ComplianceComponent);
    await init();

    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();

    complianceApi.list.and.returnValue(Promise.resolve([metRow]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('ISO-27001-A.5.1');
  });
});
