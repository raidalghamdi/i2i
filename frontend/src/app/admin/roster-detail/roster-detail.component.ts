import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { RosterApiService } from '../roster-api.service';
import { translateRosterErrorCode } from '../roster-error-messages';
import { RosterRoleDetail } from '../roster.model';

@Component({
  selector: 'app-roster-detail',
  imports: [FormsModule, DatePipe, PageHeaderComponent, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './roster-detail.component.html',
})
export class RosterDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(RosterApiService);

  private roleCode = '';
  readonly detail = signal<RosterRoleDetail | null>(null);
  readonly selectedIds = signal<Set<string>>(new Set());
  readonly inviteText = signal('');
  readonly deadlineAt = signal<string>('');
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

  private initPromise: Promise<void> | null = null;

  // Angular invokes ngOnInit automatically on the first change-detection pass; tests in this
  // codebase also await it directly to synchronize async setup before assertions. Caching the
  // in-flight promise keeps a second invocation from re-fetching/re-reloading, which matters here
  // because tests assert exact call counts on getRoleDetail after actions that reload.
  ngOnInit(): Promise<void> {
    if (!this.initPromise) {
      this.initPromise = this.doInit();
    }
    return this.initPromise;
  }

  private async doInit(): Promise<void> {
    this.roleCode = this.route.snapshot.paramMap.get('roleCode')!;
    await this.load();
  }

  retryLoad(): Promise<void> {
    return this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      await this.reload();
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@rosterDetailLoadError:Couldn't load this role. Please try again.`),
      );
    } finally {
      this.loading.set(false);
    }
  }

  private async reload(): Promise<void> {
    this.detail.set(await this.api.getRoleDetail(this.roleCode));
  }

  toggleSelected(id: string): void {
    this.selectedIds.update((set) => {
      const next = new Set(set);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  async onInvite(): Promise<void> {
    const names = this.inviteText()
      .split(/[\n,;]+/)
      .map((s) => s.trim())
      .filter((s) => s.length > 0);
    if (names.length === 0) return;
    this.errorMessage.set(null);
    try {
      const result = await this.api.invite(this.roleCode, names, this.deadlineAt() || null);
      if (result.errors.length > 0) {
        this.errorMessage.set(
          result.errors.map((e) => `${e.samAccountName}: ${translateRosterErrorCode(e.message)}`).join('; '),
        );
      }
      this.inviteText.set('');
      this.deadlineAt.set('');
      await this.reload();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onWithdraw(id: string): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.api.withdraw(id);
      await this.reload();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onBulkWithdraw(): Promise<void> {
    const ids = Array.from(this.selectedIds());
    if (ids.length === 0) return;
    this.errorMessage.set(null);
    try {
      await this.api.bulkWithdraw(ids);
      this.selectedIds.set(new Set());
      await this.reload();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onRemind(id: string): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.api.remind(id);
      await this.reload();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onBulkRemind(): Promise<void> {
    const ids = Array.from(this.selectedIds());
    if (ids.length === 0) return;
    this.errorMessage.set(null);
    try {
      await this.api.bulkRemind(ids);
      this.selectedIds.set(new Set());
      await this.reload();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  private extractErrorMessage(error: unknown, fallback = $localize`Something went wrong. Please try again.`): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return fallback;
  }
}
