import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StrategicThemesService } from '../../ideas/strategic-themes.service';
import { StrategicTheme } from '../../ideas/idea.model';
import { TracksManagerComponent } from './tracks-manager.component';

describe('TracksManagerComponent', () => {
  let fixture: ComponentFixture<TracksManagerComponent>;
  let api: jasmine.SpyObj<StrategicThemesService>;

  function setup(tracks: StrategicTheme[]): void {
    api = jasmine.createSpyObj('StrategicThemesService', ['list', 'create', 'update', 'delete']);
    api.list.and.returnValue(Promise.resolve(tracks));

    TestBed.configureTestingModule({
      imports: [TracksManagerComponent],
      providers: [{ provide: StrategicThemesService, useValue: api }],
    });
    fixture = TestBed.createComponent(TracksManagerComponent);
  }

  it('renders existing tracks', async () => {
    setup([{ id: 'theme-1', nameAr: 'ا', nameEn: 'Track One', descriptionAr: null, descriptionEn: null }]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Track One');
  });

  it('shows an empty-state message when there are no tracks', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No tracks yet.');
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    api = jasmine.createSpyObj('StrategicThemesService', ['list', 'create', 'update', 'delete']);
    api.list.and.returnValue(Promise.reject({ error: { error: 'Tracks unavailable' } }));

    TestBed.configureTestingModule({
      imports: [TracksManagerComponent],
      providers: [{ provide: StrategicThemesService, useValue: api }],
    });
    fixture = TestBed.createComponent(TracksManagerComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Tracks unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    api.list.and.returnValue(Promise.resolve([{ id: 'theme-1', nameAr: 'ا', nameEn: 'Track One', descriptionAr: null, descriptionEn: null }]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Track One');
  });

  it('rejects create when names are blank', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    await fixture.componentInstance.onCreate();

    expect(api.create).not.toHaveBeenCalled();
    expect(fixture.componentInstance.errorMessage()).not.toBeNull();
  });

  it('creates a track and refreshes the list', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    const created: StrategicTheme = { id: 'theme-2', nameAr: 'ب', nameEn: 'Track Two', descriptionAr: null, descriptionEn: null };
    api.create.and.returnValue(Promise.resolve(created));
    api.list.and.returnValue(Promise.resolve([created]));

    fixture.componentInstance.updateCreating({ nameAr: 'ب', nameEn: 'Track Two' });
    await fixture.componentInstance.onCreate();

    expect(api.create).toHaveBeenCalledWith({ nameAr: 'ب', nameEn: 'Track Two', descriptionAr: '', descriptionEn: '' });
    expect(fixture.componentInstance.tracks().length).toBe(1);
  });

  it('deletes a track and refreshes the list', async () => {
    setup([{ id: 'theme-1', nameAr: 'ا', nameEn: 'Track One', descriptionAr: null, descriptionEn: null }]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    api.delete.and.returnValue(Promise.resolve());
    api.list.and.returnValue(Promise.resolve([]));

    await fixture.componentInstance.onDelete('theme-1');

    expect(api.delete).toHaveBeenCalledWith('theme-1');
    expect(fixture.componentInstance.tracks().length).toBe(0);
  });

  it('surfaces the backend error message when delete fails', async () => {
    setup([{ id: 'theme-1', nameAr: 'ا', nameEn: 'Track One', descriptionAr: null, descriptionEn: null }]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    api.delete.and.returnValue(Promise.reject({ error: { error: 'Cannot delete a track that ideas currently reference. Reassign those ideas to another track first.' } }));

    await fixture.componentInstance.onDelete('theme-1');

    expect(fixture.componentInstance.errorMessage()).toBe('Cannot delete a track that ideas currently reference. Reassign those ideas to another track first.');
  });
});
