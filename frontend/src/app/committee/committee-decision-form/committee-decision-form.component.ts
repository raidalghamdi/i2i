import { Component, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { IdeaContextPanelComponent } from '../../shared/idea-context-panel/idea-context-panel.component';
import { CommitteeApiService } from '../committee-api.service';
import { CommitteeCriterion } from '../committee.model';
import { IdeasApiService } from '../../ideas/ideas-api.service';
import { Idea } from '../../ideas/idea.model';
import { EvaluationReviewApiService } from '../../evaluations/evaluation-review-api.service';
import { EvaluationReviewDetail } from '../../evaluations/evaluation-review.model';
import { IconComponent } from '../../shared/icon/icon.component';

// Change 20260726 — mirrors the allowlist and per-file cap enforced by idea-submit-wizard and the
// backend, so an unsupported file is rejected before it costs an upload round-trip.
const ALLOWED_ATTACHMENT_TYPES = new Set([
  'application/pdf',
  'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'application/vnd.ms-excel',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  'application/vnd.openxmlformats-officedocument.presentationml.presentation',
  'application/vnd.ms-powerpoint',
  'image/png',
  'image/jpeg',
  'video/mp4',
  'video/quicktime',
]);
const MAX_ATTACHMENT_BYTES = 10 * 1024 * 1024;

@Component({
  selector: 'app-committee-decision-form',
  imports: [ReactiveFormsModule, PageHeaderComponent, LoadingStateComponent, ErrorStateComponent, IdeaContextPanelComponent, IconComponent],
  templateUrl: './committee-decision-form.component.html',
})
export class CommitteeDecisionFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly committeeApi = inject(CommitteeApiService);
  private readonly ideasApi = inject(IdeasApiService);
  private readonly evaluationReviewApi = inject(EvaluationReviewApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly criteria = signal<CommitteeCriterion[]>([]);
  readonly idea = signal<Idea | null>(null);
  readonly detail = signal<EvaluationReviewDetail | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly queuedFiles = signal<File[]>([]); // Change 20260726
  readonly attachmentError = signal<string | null>(null); // Change 20260726
  private readonly ideaId = this.route.snapshot.paramMap.get('id')!;

  readonly form = this.fb.group({
    decisionTypeCode: this.fb.control<string>('', Validators.required),
    comments: this.fb.control<string | null>(null),
  });

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  reload(): void {
    void this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const [criteria, idea, detail] = await Promise.all([
        this.committeeApi.getCriteria(),
        this.ideasApi.getById(this.ideaId),
        this.evaluationReviewApi.getDetail(this.ideaId),
      ]);
      // `this.form` is statically typed to only know about `decisionTypeCode`/`comments` (the controls
      // declared in the `fb.group({...})` call above), so Angular 22's typed-forms `addControl` overloads
      // reject a dynamic `criterion.code` string. Widen to the untyped `FormGroup<Record<string, AbstractControl>>`
      // shape (the same shape `addControl`'s first overload expects) purely for this call; the rest of the
      // component keeps using the strongly-typed `this.form`.
      const untypedForm = this.form as unknown as FormGroup<Record<string, AbstractControl>>;
      for (const criterion of criteria) {
        if (untypedForm.contains(criterion.code)) continue;
        untypedForm.addControl(
          criterion.code,
          this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(10)]),
        );
      }
      this.criteria.set(criteria);
      this.idea.set(idea);
      this.detail.set(detail);
    } catch {
      this.loadError.set($localize`:@@committeeDecisionFormLoadError:Couldn't load the decision criteria. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }

  // Change 20260726
  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) this.queueFiles(Array.from(input.files));
    input.value = '';
  }

  // Change 20260726
  removeQueuedFile(index: number): void {
    this.queuedFiles.update((files) => files.filter((_, i) => i !== index));
  }

  // Change 20260726
  private queueFiles(files: File[]): void {
    this.attachmentError.set(null);
    for (const file of files) {
      if (!ALLOWED_ATTACHMENT_TYPES.has(file.type)) {
        this.attachmentError.set($localize`:@@committeeFormAttachmentInvalidType:One or more files have a type that isn't allowed.`);
        continue;
      }
      if (file.size > MAX_ATTACHMENT_BYTES) {
        this.attachmentError.set($localize`:@@committeeFormAttachmentTooLarge:One or more files are larger than 10MB.`);
        continue;
      }
      this.queuedFiles.update((existing) => [...existing, file]);
    }
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);

    const criteriaScores: Record<string, number> = {};
    for (const criterion of this.criteria()) {
      criteriaScores[criterion.code] = this.form.get(criterion.code)!.value as number;
    }

    try {
      await this.committeeApi.submitDecision(
        this.ideaId,
        {
          decisionTypeCode: this.form.get('decisionTypeCode')!.value as string,
          criteriaScores,
          comments: (this.form.get('comments')?.value as string | null) ?? null,
        },
        this.queuedFiles(), // Change 20260726
      );
      await this.router.navigate(['/committee/queue']);
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
