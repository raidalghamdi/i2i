import { Component, inject, signal } from '@angular/core';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { AdImportAllResult, RosterApiService } from '../roster-api.service';
import { translateRosterErrorCode } from '../roster-error-messages';
import { BulkCreateResult } from '../roster.model';

export function parseEmployeeImportCsv(text: string): { samAccountName: string; roleCode: string }[] {
  const lines = text
    .split(/\r?\n/)
    .map((l) => l.trim())
    .filter((l) => l.length > 0);
  if (lines.length === 0) return [];
  const rows = lines[0].toLowerCase().startsWith('samaccountname') ? lines.slice(1) : lines;
  return rows
    .map((line) => line.split(','))
    .filter((cols) => cols.length >= 2 && cols[0].trim().length > 0 && cols[1].trim().length > 0)
    .map((cols) => ({ samAccountName: cols[0].trim(), roleCode: cols[1].trim() }));
}

@Component({
  selector: 'app-employee-import',
  imports: [PageHeaderComponent],
  templateUrl: './employee-import.component.html',
})
export class EmployeeImportComponent {
  private readonly api = inject(RosterApiService);
  readonly result = signal<BulkCreateResult | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly translateErrorCode = translateRosterErrorCode;
  /** Result + in-flight flag for the "import all users from AD" action. */
  readonly adResult = signal<AdImportAllResult | null>(null);
  readonly adImporting = signal(false);

  async onImportAllFromAd(): Promise<void> {
    const confirmed = confirm(
      $localize`:@@employeeImportAllConfirm:Import all users from Active Directory as submitters? Accounts that already have a role are left unchanged.`,
    );
    if (!confirmed) return;
    this.adImporting.set(true);
    this.errorMessage.set(null);
    try {
      this.adResult.set(await this.api.importAllFromAd());
    } catch (error) {
      this.adResult.set(null);
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.adImporting.set(false);
    }
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    const text = await file.text();
    const rows = parseEmployeeImportCsv(text);
    try {
      this.result.set(await this.api.importEmployees(rows));
      this.errorMessage.set(null);
    } catch (error) {
      this.result.set(null);
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      input.value = '';
    }
  }

  downloadTemplate(): void {
    const blob = new Blob(['samAccountName,role\n'], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'employee-import-template.csv';
    a.click();
    URL.revokeObjectURL(url);
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`Something went wrong. Please try again.`;
  }
}
