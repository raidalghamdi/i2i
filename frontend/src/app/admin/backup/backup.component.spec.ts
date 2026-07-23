import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BackupApiService } from './backup-api.service';
import { BackupComponent } from './backup.component';

describe('BackupComponent', () => {
  let fixture: ComponentFixture<BackupComponent>;
  let backupApi: jasmine.SpyObj<BackupApiService>;

  function setup(): void {
    backupApi = jasmine.createSpyObj('BackupApiService', ['downloadBackup']);
    TestBed.configureTestingModule({
      imports: [BackupComponent],
      providers: [{ provide: BackupApiService, useValue: backupApi }],
    });
    fixture = TestBed.createComponent(BackupComponent);
  }

  it('downloads the backup blob when the button is clicked', async () => {
    setup();
    backupApi.downloadBackup.and.resolveTo(new Blob(['xlsx']));
    const createSpy = spyOn(URL, 'createObjectURL').and.returnValue('blob:x');
    const revokeSpy = spyOn(URL, 'revokeObjectURL');
    fixture.detectChanges();

    await fixture.componentInstance.onDownload();

    expect(backupApi.downloadBackup).toHaveBeenCalled();
    expect(createSpy).toHaveBeenCalled();
    expect(revokeSpy).toHaveBeenCalled();
  });

  it('surfaces an error when the download fails', async () => {
    setup();
    backupApi.downloadBackup.and.rejectWith(new Error('boom'));
    fixture.detectChanges();

    await fixture.componentInstance.onDownload();
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBeTruthy();
  });
});
