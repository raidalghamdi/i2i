import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StrategicThemesService, StrategicThemeInput } from '../../ideas/strategic-themes.service';
import { StrategicTheme } from '../../ideas/idea.model';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

interface TrackDraft {
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
}

const EMPTY_DRAFT: TrackDraft = { nameAr: '', nameEn: '', descriptionAr: '', descriptionEn: '' };

@Component({
  selector: 'app-tracks-manager',
  imports: [FormsModule, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './tracks-manager.component.html',
})
export class TracksManagerComponent implements OnInit {
  private readonly themesApi = inject(StrategicThemesService);

  readonly tracks = signal<StrategicTheme[]>([]);
  readonly creating = signal<TrackDraft>({ ...EMPTY_DRAFT });
  readonly editingId = signal<string | null>(null);
  readonly editDraft = signal<TrackDraft>({ ...EMPTY_DRAFT });
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

  ngOnInit(): Promise<void> {
    return this.load();
  }

  reload(): Promise<void> {
    return this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      this.tracks.set(await this.themesApi.list());
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@tracksManagerLoadError:Couldn't load tracks. Please try again.`),
      );
    } finally {
      this.loading.set(false);
    }
  }

  updateCreating(patch: Partial<TrackDraft>): void {
    this.creating.update((d) => ({ ...d, ...patch }));
  }

  updateEditDraft(patch: Partial<TrackDraft>): void {
    this.editDraft.update((d) => ({ ...d, ...patch }));
  }

  async onCreate(): Promise<void> {
    const draft = this.creating();
    if (!draft.nameAr.trim() || !draft.nameEn.trim()) {
      this.errorMessage.set($localize`Arabic and English names are required.`);
      return;
    }
    this.errorMessage.set(null);
    try {
      await this.themesApi.create(draft as StrategicThemeInput);
      this.tracks.set(await this.themesApi.list());
      this.creating.set({ ...EMPTY_DRAFT });
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  startEdit(track: StrategicTheme): void {
    this.editingId.set(track.id);
    this.editDraft.set({
      nameAr: track.nameAr,
      nameEn: track.nameEn,
      descriptionAr: track.descriptionAr ?? '',
      descriptionEn: track.descriptionEn ?? '',
    });
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  async saveEdit(id: string): Promise<void> {
    const draft = this.editDraft();
    if (!draft.nameAr.trim() || !draft.nameEn.trim()) {
      this.errorMessage.set($localize`Arabic and English names are required.`);
      return;
    }
    this.errorMessage.set(null);
    try {
      await this.themesApi.update(id, draft as StrategicThemeInput);
      this.tracks.set(await this.themesApi.list());
      this.editingId.set(null);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onDelete(id: string): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.themesApi.delete(id);
      this.tracks.set(await this.themesApi.list());
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
