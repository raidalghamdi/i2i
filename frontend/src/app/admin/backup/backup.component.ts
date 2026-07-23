import { Component, inject, signal } from '@angular/core';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { BackupApiService } from './backup-api.service';

@Component({
  selector: 'app-backup',
  imports: [PageHeaderComponent],
  templateUrl: './backup.component.html',
})
export class BackupComponent {
  private readonly backupApi = inject(BackupApiService);

  readonly downloading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  async onDownload(): Promise<void> {
    this.errorMessage.set(null);
    this.downloading.set(true);
    try {
      const blob = await this.backupApi.downloadBackup();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = 'backup.xlsx';
      anchor.click();
      URL.revokeObjectURL(url);
    } catch {
      this.errorMessage.set($localize`:@@backupDownloadError:Could not generate the backup. Please try again.`);
    } finally {
      this.downloading.set(false);
    }
  }
}
