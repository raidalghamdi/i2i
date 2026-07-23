import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MeApiService } from '../core/me-api.service';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../shared/error-state/error-state.component';

export interface EvaluatorLevel {
  index: number;
  thresholdPoints: number;
  nameAr: string;
  nameEn: string;
}

const EVALUATOR_LEVELS: EvaluatorLevel[] = [
  { index: 1, thresholdPoints: 0, nameAr: 'مقيّم مبتدئ', nameEn: 'Novice Evaluator' },
  { index: 2, thresholdPoints: 50, nameAr: 'مقيّم نشط', nameEn: 'Active Evaluator' },
  { index: 3, thresholdPoints: 150, nameAr: 'مقيّم متمرس', nameEn: 'Seasoned Evaluator' },
  { index: 4, thresholdPoints: 300, nameAr: 'مقيّم خبير', nameEn: 'Expert Evaluator' },
  { index: 5, thresholdPoints: 500, nameAr: 'مقيّم متميز', nameEn: 'Distinguished Evaluator' },
];

@Component({
  selector: 'app-evaluator-level',
  imports: [PageHeaderComponent, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './evaluator-level.component.html',
})
export class EvaluatorLevelComponent implements OnInit {
  private readonly meApi = inject(MeApiService);

  readonly levels = signal<EvaluatorLevel[]>(EVALUATOR_LEVELS);
  readonly points = signal(0);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

  readonly currentLevel = computed<EvaluatorLevel>(() => {
    const pts = this.points();
    let current = this.levels()[0];
    for (const level of this.levels()) {
      if (level.thresholdPoints <= pts) current = level;
    }
    return current;
  });

  readonly nextLevel = computed<EvaluatorLevel | null>(() => {
    const currentIndex = this.currentLevel().index;
    return this.levels().find((l) => l.index === currentIndex + 1) ?? null;
  });

  readonly pointsToNext = computed<number>(() => {
    const next = this.nextLevel();
    return next ? Math.max(0, next.thresholdPoints - this.points()) : 0;
  });

  readonly progressPercent = computed<number>(() => {
    const next = this.nextLevel();
    if (!next) return 100;
    const current = this.currentLevel();
    const span = next.thresholdPoints - current.thresholdPoints;
    if (span <= 0) return 100;
    return Math.min(100, Math.max(0, ((this.points() - current.thresholdPoints) / span) * 100));
  });

  ngOnInit(): Promise<void> {
    return this.refresh();
  }

  async refresh(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const me = await this.meApi.get();
      this.points.set(me.points);
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@evaluatorLevelLoadError:Couldn't load your level. Please try again.`),
      );
    } finally {
      this.loading.set(false);
    }
  }

  isReached(level: EvaluatorLevel): boolean {
    return level.index <= this.currentLevel().index;
  }

  private extractErrorMessage(error: unknown, fallback = $localize`Something went wrong. Please try again.`): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return fallback;
  }
}
