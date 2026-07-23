import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RosterApiService } from '../roster-api.service';
import { BulkCreateResult } from '../roster.model';
import { EmployeeImportComponent, parseEmployeeImportCsv } from './employee-import.component';

describe('parseEmployeeImportCsv', () => {
  it('parses a CSV with a samAccountName,role header row into rows, skipping the header', () => {
    const csv = 'samAccountName,role\njsmith,evaluator\nabrown,supervisor';

    expect(parseEmployeeImportCsv(csv)).toEqual([
      { samAccountName: 'jsmith', roleCode: 'evaluator' },
      { samAccountName: 'abrown', roleCode: 'supervisor' },
    ]);
  });

  it('parses a header-less CSV (first row is not recognized as a header)', () => {
    const csv = 'jsmith,evaluator\nabrown,supervisor';

    expect(parseEmployeeImportCsv(csv)).toEqual([
      { samAccountName: 'jsmith', roleCode: 'evaluator' },
      { samAccountName: 'abrown', roleCode: 'supervisor' },
    ]);
  });

  it('trims whitespace and skips blank lines and rows missing a column', () => {
    const csv = 'samAccountName,role\n  jsmith , evaluator \n\nbaduser\n,supervisor\nabrown,';

    expect(parseEmployeeImportCsv(csv)).toEqual([{ samAccountName: 'jsmith', roleCode: 'evaluator' }]);
  });

  it('handles Windows-style CRLF line endings', () => {
    const csv = 'samAccountName,role\r\njsmith,evaluator\r\nabrown,supervisor';

    expect(parseEmployeeImportCsv(csv)).toEqual([
      { samAccountName: 'jsmith', roleCode: 'evaluator' },
      { samAccountName: 'abrown', roleCode: 'supervisor' },
    ]);
  });

  it('returns an empty array for empty input', () => {
    expect(parseEmployeeImportCsv('')).toEqual([]);
    expect(parseEmployeeImportCsv('   \n  \n')).toEqual([]);
  });
});

describe('EmployeeImportComponent', () => {
  let fixture: ComponentFixture<EmployeeImportComponent>;
  let api: jasmine.SpyObj<RosterApiService>;

  function setup(): void {
    api = jasmine.createSpyObj('RosterApiService', ['importEmployees']);
    TestBed.configureTestingModule({
      imports: [EmployeeImportComponent],
      providers: [{ provide: RosterApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(EmployeeImportComponent);
  }

  function selectFileEvent(text: string, name = 'employees.csv'): Event {
    const file = new File([text], name, { type: 'text/csv' });
    const input = document.createElement('input');
    input.type = 'file';
    Object.defineProperty(input, 'files', { value: [file], configurable: true });
    return { target: input } as unknown as Event;
  }

  it('parses the selected CSV file and calls importEmployees with the parsed rows', async () => {
    setup();
    fixture.detectChanges();
    const result: BulkCreateResult = { total: 2, created: 2, skipped: 0, errors: [] };
    api.importEmployees.and.returnValue(Promise.resolve(result));

    await fixture.componentInstance.onFileSelected(
      selectFileEvent('samAccountName,role\njsmith,evaluator\nabrown,supervisor'),
    );

    expect(api.importEmployees).toHaveBeenCalledWith([
      { samAccountName: 'jsmith', roleCode: 'evaluator' },
      { samAccountName: 'abrown', roleCode: 'supervisor' },
    ]);
  });

  it('renders the 3-tile summary and a per-row error list after a successful import', async () => {
    setup();
    fixture.detectChanges();
    const result: BulkCreateResult = {
      total: 3,
      created: 2,
      skipped: 1,
      errors: [{ samAccountName: 'baduser', message: 'Not found in AD' }],
    };
    api.importEmployees.and.returnValue(Promise.resolve(result));

    await fixture.componentInstance.onFileSelected(
      selectFileEvent('samAccountName,role\njsmith,evaluator\nabrown,supervisor\nbaduser,evaluator'),
    );
    fixture.detectChanges();

    expect(fixture.componentInstance.result()).toEqual(result);
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('3');
    expect(text).toContain('2');
    expect(text).toContain('1');
    expect(text).toContain('baduser');
    expect(text).toContain('Not found in AD');
  });

  it('surfaces a backend error message when importEmployees rejects', async () => {
    setup();
    fixture.detectChanges();
    // Deliberately created lazily (not via `.and.returnValue(Promise.reject(...))`): this
    // component does a real async `file.text()` read before calling importEmployees, so a
    // pre-constructed rejected promise sits unhandled long enough for Chrome to report an
    // "unhandled promise rejection" before the component's catch attaches. Creating the
    // rejection only once importEmployees is actually invoked keeps the attach synchronous.
    api.importEmployees.and.callFake(() => Promise.reject({ error: { error: 'Malformed rows' } }));

    await fixture.componentInstance.onFileSelected(selectFileEvent('samAccountName,role\njsmith,evaluator'));
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBe('Malformed rows');
    expect(fixture.componentInstance.result()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Malformed rows');
  });

  it('does nothing when no file is selected', async () => {
    setup();
    fixture.detectChanges();
    const input = document.createElement('input');
    input.type = 'file';

    await fixture.componentInstance.onFileSelected({ target: input } as unknown as Event);

    expect(api.importEmployees).not.toHaveBeenCalled();
  });

  it('generates a downloadable CSV template with the correct header row', async () => {
    setup();
    fixture.detectChanges();

    let capturedBlob: Blob | null = null;
    spyOn(URL, 'createObjectURL').and.callFake((obj: Blob | MediaSource) => {
      capturedBlob = obj as Blob;
      return 'blob:mock-url';
    });
    spyOn(URL, 'revokeObjectURL');

    fixture.componentInstance.downloadTemplate();

    expect(URL.createObjectURL).toHaveBeenCalled();
    expect(capturedBlob).not.toBeNull();
    const text = await capturedBlob!.text();
    expect(text).toBe('samAccountName,role\n');
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:mock-url');
  });
});
