import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { IdeasApiService } from '../ideas-api.service';
import { StrategicThemesService } from '../strategic-themes.service';
import { IdeaInput, StrategicTheme } from '../idea.model';

@Component({
  selector: 'app-idea-form',
  imports: [ReactiveFormsModule, PageHeaderComponent, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './idea-form.component.html',
})
export class IdeaFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ideasApi = inject(IdeasApiService);
  private readonly themesApi = inject(StrategicThemesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly themes = signal<StrategicTheme[]>([]);
  readonly queuedFiles = signal<File[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  private ideaId: string | null = null;

  readonly form = this.fb.nonNullable.group({
    titleAr: ['', Validators.required],
    titleEn: ['', Validators.required],
    problemStatementAr: ['', Validators.required],
    problemStatementEn: ['', Validators.required],
    proposedSolutionAr: ['', Validators.required],
    proposedSolutionEn: ['', Validators.required],
    expectedBenefitsAr: ['', Validators.required],
    expectedBenefitsEn: ['', Validators.required],
    strategicThemeId: ['', Validators.required],
  });

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async reload(): Promise<void> {
    await this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      this.themes.set(await this.themesApi.list());

      const id = this.route.snapshot.paramMap.get('id');
      if (id) {
        this.ideaId = id;
        const idea = await this.ideasApi.getById(id);
        this.form.patchValue({
          titleAr: idea.titleAr,
          titleEn: idea.titleEn,
          problemStatementAr: idea.problemStatementAr,
          problemStatementEn: idea.problemStatementEn,
          proposedSolutionAr: idea.proposedSolutionAr,
          proposedSolutionEn: idea.proposedSolutionEn,
          expectedBenefitsAr: idea.expectedBenefitsAr,
          expectedBenefitsEn: idea.expectedBenefitsEn,
          strategicThemeId: idea.strategicThemeId,
        });
      }
    } catch {
      this.loadError.set($localize`:@@ideaFormLoadError:Couldn't load the idea. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.queuedFiles.update((files) => [...files, ...Array.from(input.files!)]);
      input.value = '';
    }
  }

  removeQueuedFile(index: number): void {
    this.queuedFiles.update((files) => files.filter((_, i) => i !== index));
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);
    const input = this.form.getRawValue();

    try {
      if (this.ideaId) {
        await this.ideasApi.update(this.ideaId, input as unknown as IdeaInput);
      } else {
        const created = await this.ideasApi.create(input as unknown as IdeaInput);
        this.ideaId = created.id;
      }
      await this.uploadQueuedFiles(this.ideaId);
      await this.router.navigate(['/ideas', this.ideaId]);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  private async uploadQueuedFiles(id: string): Promise<void> {
    while (this.queuedFiles().length > 0) {
      const file = this.queuedFiles()[0];
      await this.ideasApi.uploadAttachment(id, file);
      this.queuedFiles.update((files) => files.slice(1));
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
