import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { IdeasApiService } from '../ideas-api.service';
import { StrategicThemesService } from '../strategic-themes.service';
import { ActivitiesService } from '../activities.service';
import { ChallengesService } from '../challenges.service';
import { Activity, Challenge, IdeaInput, StrategicTheme, TeamMemberInput } from '../idea.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { DirectoryPickerComponent } from '../../shared/directory-picker/directory-picker.component';
import { DirectoryPerson } from '../../core/directory-api.service';
import { TermsModalComponent } from '../terms-modal/terms-modal.component';

const MIN_ADDITIONAL_TEAM_MEMBERS = 1;
const MAX_ADDITIONAL_TEAM_MEMBERS = 4;
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
const MAX_ATTACHMENT_COUNT = 5;
const WIZARD_DRAFT_KEY = 'i2i-idea-draft-v1';

interface DraftShape {
  title: string;
  description: string;
  strategicThemeId: string;
  activityId: string;
}

@Component({
  selector: 'app-idea-submit-wizard',
  imports: [ReactiveFormsModule, PageHeaderComponent, IconComponent, LoadingStateComponent, ErrorStateComponent, DirectoryPickerComponent, TermsModalComponent],
  templateUrl: './idea-submit-wizard.component.html',
})
export class IdeaSubmitWizardComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  protected readonly ideasApi = inject(IdeasApiService);
  private readonly themesApi = inject(StrategicThemesService);
  private readonly activitiesApi = inject(ActivitiesService);
  private readonly challengesApi = inject(ChallengesService);
  protected readonly router = inject(Router);

  readonly stepLabels = [
    $localize`:@@ideaWizardStepBasics:Basics`,
    $localize`:@@ideaWizardStepDetails:Details`,
    $localize`:@@ideaWizardStepAttachments:Attachments`,
    $localize`:@@ideaWizardStepReview:Review`,
  ];

  readonly currentStep = signal(0);
  readonly themes = signal<StrategicTheme[]>([]);
  readonly activities = signal<Activity[]>([]);
  readonly challengeOptions = signal<Challenge[]>([]);
  readonly teamMembers = signal<TeamMemberInput[]>([]);
  readonly queuedFiles = signal<File[]>([]);
  readonly attachmentError = signal<string | null>(null);
  readonly dragActive = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly draftAvailable = signal(false);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly termsModalOpen = signal(false);

  protected createdIdeaId: string | null = null;
  private draftSaveTimeout: ReturnType<typeof setTimeout> | undefined;

  readonly form = this.fb.nonNullable.group({
    basics: this.fb.nonNullable.group({
      activityId: ['', Validators.required],
      strategicThemeId: ['', Validators.required],
      challengeId: [''],
      participationType: ['individual' as 'individual' | 'team', Validators.required],
      teamName: [''],
    }),
    details: this.fb.nonNullable.group({
      title: ['', [Validators.required, Validators.maxLength(120)]],
      description: ['', [Validators.required, Validators.maxLength(2000)]],
      problemStatementAr: ['', Validators.required],
      expectedBenefitsAr: ['', Validators.required],
    }),
    review: this.fb.nonNullable.group({
      ipAcknowledged: [false, Validators.requiredTrue],
      termsAgreed: [false, Validators.requiredTrue],
    }),
  });

  async ngOnInit(): Promise<void> {
    this.form.controls.basics.controls.strategicThemeId.valueChanges.subscribe((themeId) => {
      this.onThemeChange(themeId);
    });

    this.form.controls.details.valueChanges.subscribe(() => this.scheduleDraftSave());
    this.form.controls.basics.controls.strategicThemeId.valueChanges.subscribe(() => this.scheduleDraftSave());
    this.form.controls.basics.controls.activityId.valueChanges.subscribe(() => this.scheduleDraftSave());

    if (localStorage.getItem(WIZARD_DRAFT_KEY)) {
      this.draftAvailable.set(true);
    }

    await this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      this.themes.set(await this.themesApi.list());
      this.activities.set(await this.activitiesApi.list());
    } catch {
      this.loadError.set($localize`:@@ideaWizardLoadError:Couldn't load the idea submission form. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }

  ngOnDestroy(): void {
    if (this.draftSaveTimeout) clearTimeout(this.draftSaveTimeout);
  }

  private async onThemeChange(themeId: string): Promise<void> {
    this.form.controls.basics.controls.challengeId.setValue('');
    if (!themeId) {
      this.challengeOptions.set([]);
      return;
    }
    this.challengeOptions.set(await this.challengesApi.listByTheme(themeId));
  }

  onTeamMembersSelected(people: DirectoryPerson | DirectoryPerson[]): void {
    const list = Array.isArray(people) ? people : [people];
    this.teamMembers.set(list.map((p) => ({ samAccountName: p.samAccountName, name: p.displayName, email: p.email })));
  }

  private scheduleDraftSave(): void {
    if (this.draftSaveTimeout) clearTimeout(this.draftSaveTimeout);
    this.draftSaveTimeout = setTimeout(() => this.saveDraft(), 700);
  }

  private saveDraft(): void {
    const draft: DraftShape = {
      title: this.form.controls.details.controls.title.value,
      description: this.form.controls.details.controls.description.value,
      strategicThemeId: this.form.controls.basics.controls.strategicThemeId.value,
      activityId: this.form.controls.basics.controls.activityId.value,
    };
    localStorage.setItem(WIZARD_DRAFT_KEY, JSON.stringify(draft));
  }

  restoreDraft(): void {
    const raw = localStorage.getItem(WIZARD_DRAFT_KEY);
    if (!raw) return;
    const draft = JSON.parse(raw) as DraftShape;
    this.form.controls.details.patchValue({ title: draft.title, description: draft.description });
    this.form.controls.basics.patchValue({ strategicThemeId: draft.strategicThemeId, activityId: draft.activityId });
    this.draftAvailable.set(false);
  }

  dismissDraft(): void {
    localStorage.removeItem(WIZARD_DRAFT_KEY);
    this.draftAvailable.set(false);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragActive.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.dragActive.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragActive.set(false);
    if (event.dataTransfer?.files) this.queueFiles(Array.from(event.dataTransfer.files));
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) this.queueFiles(Array.from(input.files));
    input.value = '';
  }

  removeQueuedFile(index: number): void {
    this.queuedFiles.update((files) => files.filter((_, i) => i !== index));
  }

  private queueFiles(files: File[]): void {
    this.attachmentError.set(null);
    for (const file of files) {
      if (this.queuedFiles().length >= MAX_ATTACHMENT_COUNT) {
        this.attachmentError.set($localize`:@@ideaWizardAttachmentMaxCount:You can attach up to 5 files.`);
        break;
      }
      if (!ALLOWED_ATTACHMENT_TYPES.has(file.type)) {
        this.attachmentError.set($localize`:@@ideaWizardAttachmentInvalidType:One or more files have a type that isn't allowed.`);
        continue;
      }
      if (file.size > MAX_ATTACHMENT_BYTES) {
        this.attachmentError.set($localize`:@@ideaWizardAttachmentTooLarge:One or more files are larger than 10MB.`);
        continue;
      }
      this.queuedFiles.update((existing) => [...existing, file]);
    }
  }

  stepValid(index: number): boolean {
    switch (index) {
      case 0:
        return this.basicsStepValid();
      case 1:
        return this.form.controls.details.valid;
      case 2:
        return this.queuedFiles().length >= 1;
      case 3:
        return this.form.controls.review.valid;
      default:
        return true;
    }
  }

  private basicsStepValid(): boolean {
    const basics = this.form.controls.basics;
    if (!basics.controls.activityId.valid || !basics.controls.strategicThemeId.valid || !basics.controls.participationType.valid) {
      return false;
    }
    if (this.challengeOptions().length > 0 && !basics.controls.challengeId.value) {
      return false;
    }
    if (basics.controls.participationType.value === 'team') {
      if (!basics.controls.teamName.value.trim()) return false;
      const count = this.teamMembers().length;
      if (count < MIN_ADDITIONAL_TEAM_MEMBERS || count > MAX_ADDITIONAL_TEAM_MEMBERS) return false;
    }
    return true;
  }

  goNext(): void {
    if (!this.stepValid(this.currentStep())) return;
    this.currentStep.update((s) => Math.min(s + 1, this.stepLabels.length - 1));
  }

  goBack(): void {
    this.currentStep.update((s) => Math.max(s - 1, 0));
  }

  openTermsModal(event: Event): void {
    // Prevent this button-inside-<label> click from also toggling the
    // "termsAgreed" checkbox via the label's default click-forwarding.
    event.preventDefault();
    event.stopPropagation();
    this.termsModalOpen.set(true);
  }

  closeTermsModal(): void {
    this.termsModalOpen.set(false);
  }

  selectedThemeName(): string {
    const theme = this.themes().find((t) => t.id === this.form.controls.basics.controls.strategicThemeId.value);
    return theme?.nameAr ?? '';
  }

  selectedActivityName(): string {
    const activity = this.activities().find((a) => a.id === this.form.controls.basics.controls.activityId.value);
    return activity?.nameAr ?? '';
  }

  selectedChallengeText(): string {
    const challenge = this.challengeOptions().find((c) => c.id === this.form.controls.basics.controls.challengeId.value);
    return challenge?.textAr ?? '';
  }

  protected extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`Something went wrong. Please try again.`;
  }

  async onSubmit(): Promise<void> {
    if (!this.stepValid(3)) return;
    this.errorMessage.set(null);
    this.submitting.set(true);

    try {
      const input = this.buildIdeaInput();
      if (!this.createdIdeaId) {
        const created = await this.ideasApi.create(input);
        this.createdIdeaId = created.id;
      } else {
        await this.ideasApi.update(this.createdIdeaId, input);
      }

      await this.uploadQueuedFiles(this.createdIdeaId);
      await this.ideasApi.submit(this.createdIdeaId);

      this.clearDraft();
      await this.router.navigate(['/ideas', this.createdIdeaId, 'submitted']);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.submitting.set(false);
    }
  }

  private buildIdeaInput(): IdeaInput {
    const basics = this.form.controls.basics.getRawValue();
    const details = this.form.controls.details.getRawValue();
    const review = this.form.controls.review.getRawValue();
    const isTeam = basics.participationType === 'team';

    return {
      titleAr: details.title,
      titleEn: details.title,
      problemStatementAr: details.problemStatementAr,
      problemStatementEn: details.problemStatementAr,
      proposedSolutionAr: details.description,
      proposedSolutionEn: details.description,
      expectedBenefitsAr: details.expectedBenefitsAr,
      expectedBenefitsEn: details.expectedBenefitsAr,
      strategicThemeId: basics.strategicThemeId,
      activityId: basics.activityId,
      challengeId: basics.challengeId || null,
      participationType: basics.participationType,
      teamName: isTeam ? basics.teamName : null,
      teamMembers: isTeam ? this.teamMembers() : [],
      ipAcknowledged: review.ipAcknowledged,
      termsAgreed: review.termsAgreed,
    };
  }

  private async uploadQueuedFiles(id: string): Promise<void> {
    while (this.queuedFiles().length > 0) {
      const file = this.queuedFiles()[0];
      await this.ideasApi.uploadAttachment(id, file);
      this.queuedFiles.update((files) => files.slice(1));
    }
  }

  private clearDraft(): void {
    if (this.draftSaveTimeout) clearTimeout(this.draftSaveTimeout);
    localStorage.removeItem(WIZARD_DRAFT_KEY);
  }
}
