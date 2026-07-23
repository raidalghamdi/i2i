import { Component, Inject, LOCALE_ID, OnInit, computed, inject, signal } from '@angular/core';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { ComplianceApiService } from '../compliance-api.service';
import { ComplianceControlRow } from '../compliance.model';

interface ComplianceGroup {
  standardBodyCode: string;
  name: string;
  rows: ComplianceControlRow[];
}

@Component({
  selector: 'app-compliance',
  imports: [PageHeaderComponent, StatusBadgeComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent, IconComponent],
  templateUrl: './compliance.component.html',
})
export class ComplianceComponent implements OnInit {
  private readonly complianceApi = inject(ComplianceApiService);
  private readonly isArabic: boolean;

  readonly rows = signal<ComplianceControlRow[]>([]);
  readonly loading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  /** Rows grouped by standard body (SDAIA/NDMO, NCA, …), preserving the
   * API's own ordering (it sorts by StandardBody.SortOrder then ControlCode) —
   * a faithful port of the legacy compliance register's grouped layout. */
  readonly groups = computed<ComplianceGroup[]>(() => {
    const byCode = new Map<string, ComplianceGroup>();
    for (const row of this.rows()) {
      const existing = byCode.get(row.standardBodyCode);
      if (existing) {
        existing.rows.push(row);
      } else {
        byCode.set(row.standardBodyCode, { standardBodyCode: row.standardBodyCode, name: this.standardBody(row), rows: [row] });
      }
    }
    return [...byCode.values()];
  });

  readonly compliantCount = computed(() => this.rows().filter((r) => r.statusCode === 'met').length);
  readonly inProgressCount = computed(() => this.rows().filter((r) => r.statusCode === 'in_progress').length);
  readonly nonCompliantCount = computed(() => this.rows().length - this.compliantCount() - this.inProgressCount());

  readonly compliantLabel = $localize`:@@complianceKpiCompliant:Compliant`;
  readonly inProgressLabel = $localize`:@@complianceKpiInProgress:In progress`;
  readonly nonCompliantLabel = $localize`:@@complianceKpiNonCompliant:Non-compliant`;

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.errorMessage.set(null);
    this.loading.set(true);
    try {
      this.rows.set(await this.complianceApi.list());
    } catch {
      this.errorMessage.set(
        $localize`:@@complianceLoadError:Could not load compliance controls. Please try again.`,
      );
    } finally {
      this.loading.set(false);
    }
  }

  title(row: ComplianceControlRow): string {
    return this.isArabic ? row.titleAr : row.titleEn;
  }

  standardBody(row: ComplianceControlRow): string {
    return this.isArabic ? row.standardBodyNameAr : row.standardBodyNameEn;
  }

  statusName(row: ComplianceControlRow): string {
    return this.isArabic ? row.statusNameAr : row.statusNameEn;
  }
}
