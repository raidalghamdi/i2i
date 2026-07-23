import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ReportTitlesApiService } from './report-titles-api.service';
import { ReportTitle } from './report-titles.model';
import { ReportTitlesComponent } from './report-titles.component';

describe('ReportTitlesComponent', () => {
  let fixture: ComponentFixture<ReportTitlesComponent>;
  let api: jasmine.SpyObj<ReportTitlesApiService>;

  const titleA: ReportTitle = {
    id: 'title-1',
    key: 'idea-summary',
    titleAr: 'ملخص الفكرة',
    titleEn: 'Idea Summary',
    sortOrder: 1,
  };

  const titleB: ReportTitle = {
    id: 'title-2',
    key: 'committee-report',
    titleAr: 'تقرير اللجنة',
    titleEn: 'Committee Report',
    sortOrder: 2,
  };

  async function setup(titles: ReportTitle[]): Promise<void> {
    api = jasmine.createSpyObj('ReportTitlesApiService', ['list', 'create', 'update', 'remove']);
    api.list.and.returnValue(Promise.resolve(titles));
    api.create.and.returnValue(Promise.resolve({ id: 'title-3' }));
    api.update.and.returnValue(Promise.resolve());
    api.remove.and.returnValue(Promise.resolve());

    await TestBed.configureTestingModule({
      imports: [ReportTitlesComponent],
      providers: [provideRouter([]), { provide: ReportTitlesApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(ReportTitlesComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders one row per report title', async () => {
    await setup([titleA, titleB]);

    const keyCells = Array.from(
      fixture.nativeElement.querySelectorAll('tbody tr td:first-child'),
    ) as HTMLElement[];
    expect(keyCells.map((cell) => cell.textContent?.trim())).toEqual(['idea-summary', 'committee-report']);
  });

  it('shows an empty-state message when there are no report titles', async () => {
    await setup([]);

    expect(fixture.nativeElement.textContent).toContain('No report titles yet');
  });

  it('saves an edited row via update() with the edited titles/sort order and reloads', async () => {
    await setup([titleA, titleB]);

    const row = fixture.componentInstance.editableRows()[0];
    fixture.componentInstance.updateRow(row.id, { titleEn: 'Idea Summary (revised)', sortOrder: 5 });

    const updated = { ...titleA, titleEn: 'Idea Summary (revised)', sortOrder: 5 };
    api.list.and.returnValue(Promise.resolve([updated, titleB]));

    await fixture.componentInstance.onSave(fixture.componentInstance.editableRows()[0]);

    expect(api.update).toHaveBeenCalledWith('title-1', {
      titleAr: 'ملخص الفكرة',
      titleEn: 'Idea Summary (revised)',
      sortOrder: 5,
    });
    expect(api.list).toHaveBeenCalledTimes(2);
  });

  it('does not allow editing the key of an existing row', async () => {
    await setup([titleA, titleB]);

    const keyInputs = fixture.nativeElement.querySelectorAll('tbody tr input[name="key"]');
    expect(keyInputs.length).toBe(0);
  });

  it('deletes a row via remove() and reloads', async () => {
    await setup([titleA, titleB]);

    api.list.and.returnValue(Promise.resolve([titleB]));

    await fixture.componentInstance.onDelete('title-1');

    expect(api.remove).toHaveBeenCalledWith('title-1');
    expect(api.list).toHaveBeenCalledTimes(2);
  });

  it('creates a new title via create() using the add-form fields and reloads', async () => {
    await setup([titleA]);

    fixture.componentInstance.newKey.set('new-report');
    fixture.componentInstance.newTitleEn.set('New Report');
    fixture.componentInstance.newTitleAr.set('تقرير جديد');
    fixture.componentInstance.newSortOrder.set(3);

    const created = { id: 'title-1', key: 'idea-summary', titleAr: 'ملخص الفكرة', titleEn: 'Idea Summary', sortOrder: 1 };
    api.list.and.returnValue(
      Promise.resolve([created, { id: 'title-3', key: 'new-report', titleAr: 'تقرير جديد', titleEn: 'New Report', sortOrder: 3 }]),
    );

    await fixture.componentInstance.onAdd();

    expect(api.create).toHaveBeenCalledWith({
      key: 'new-report',
      titleAr: 'تقرير جديد',
      titleEn: 'New Report',
      sortOrder: 3,
    });
    expect(api.list).toHaveBeenCalledTimes(2);
    expect(fixture.componentInstance.newKey()).toBe('');
  });

  it('surfaces an error message when save fails', async () => {
    await setup([titleA, titleB]);

    api.update.and.returnValue(Promise.reject({ error: { error: 'Sort order is required.' } }));

    await fixture.componentInstance.onSave(fixture.componentInstance.editableRows()[0]);

    expect(fixture.componentInstance.errorMessage()).toBe('Sort order is required.');
  });

  it('shows an error state with retry when the list call fails, and recovers on retry', async () => {
    api = jasmine.createSpyObj('ReportTitlesApiService', ['list', 'create', 'update', 'remove']);
    api.list.and.returnValue(Promise.reject({ error: { error: 'boom' } }));

    await TestBed.configureTestingModule({
      imports: [ReportTitlesComponent],
      providers: [provideRouter([]), { provide: ReportTitlesApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(ReportTitlesComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('boom');
    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();

    api.list.and.returnValue(Promise.resolve([titleA]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('idea-summary');
  });
});
