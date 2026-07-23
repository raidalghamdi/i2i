import { Component, inject, signal } from '@angular/core';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';
import { SupervisorApiService } from '../../supervisor/supervisor-api.service';
import { FinalRankingResult } from '../../supervisor/supervisor.model';

@Component({
  selector: 'app-final-ranking-panel',
  imports: [StatusBadgeComponent],
  templateUrl: './final-ranking-panel.component.html',
})
export class FinalRankingPanelComponent {
  private readonly supervisorApi = inject(SupervisorApiService);

  readonly preview = signal<FinalRankingResult | null>(null);
  readonly result = signal<FinalRankingResult | null>(null);
  readonly errorMessage = signal<string | null>(null);

  async onPreview(): Promise<void> {
    this.errorMessage.set(null);
    this.result.set(null);
    try {
      this.preview.set(await this.supervisorApi.previewFinalRanking());
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onRun(): Promise<void> {
    this.errorMessage.set(null);
    this.preview.set(null);
    try {
      this.result.set(await this.supervisorApi.runFinalRanking());
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`Something went wrong. Please try again.`;
  }
}
