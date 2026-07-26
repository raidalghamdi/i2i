import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { DirectoryPickerComponent } from '../../shared/directory-picker/directory-picker.component';
import { DirectoryPerson } from '../../core/directory-api.service';
import { IdeasApiService } from '../ideas-api.service';
import { StrategicThemesService } from '../strategic-themes.service';
import { ActivitiesService } from '../activities.service';
import { ChallengesService } from '../challenges.service';
import {
  Activity,
  Challenge,
  Idea,
  IdeaAttachment,
  IdeaInput,
  IdeaResubmitInput,
  StrategicTheme,
  TeamMemberInput,
} from '../idea.model';

/**
 * Maps each reactive-form control to the `editableSections` key a supervisor "return" decision uses
 * to unlock it (see `shared/section-multiselect`). Every idea field is individually unlockable.
 */
const SECTION_KEY_BY_CONTROL: Record<string, string> = {
  titleAr: 'title',
  problemStatementAr: 'problem_statement',
  proposedSolutionAr: 'proposed_solution',
  expectedBenefitsAr: 'expected_benefits',
  strategicThemeId: 'strategic_theme_id',
  activityId: 'activity_id',
  challengeId: 'challenge',
  participationType: 'participation_type',
  teamName: 'team',
};

/** All section keys — a returned idea with an empty `editableSections` allows every section. */
const ALL_SECTIONS = [
  'title',
  'problem_statement',
  'proposed_solution',
  'expected_benefits',
  'activity_id',
  'strategic_theme_id',
  'challenge',
  'participation_type',
  'team',
  'attachments',
];

@Component({
  selector: 'app-idea-form',
  imports: [ReactiveFormsModule, PageHeaderComponent, LoadingStateComponent, ErrorStateComponent, DirectoryPickerComponent],
  templateUrl: './idea-form.component.html',
})
export class IdeaFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ideasApi = inject(IdeasApiService);
  private readonly themesApi = inject(StrategicThemesService);
  private readonly activitiesApi = inject(ActivitiesService);
  private readonly challengesApi = inject(ChallengesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly themes = signal<StrategicTheme[]>([]);
  readonly activities = signal<Activity[]>([]);
  readonly challengeOptions = signal<Challenge[]>([]);
  readonly teamMembers = signal<TeamMemberInput[]>([]);
  readonly initialTeam = signal<DirectoryPerson[]>([]);
  readonly existingAttachments = signal<IdeaAttachment[]>([]);
  readonly queuedFiles = signal<File[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  /** True when the idea was returned by a supervisor — sections are individually locked. */
  readonly isReturned = signal(false);
  private readonly allowedSections = signal<Set<string>>(new Set());

  private ideaId: string | null = null;
  private loadedIdea: Idea | null = null;

  readonly form = this.fb.nonNullable.group({
    titleAr: ['', Validators.required],
    problemStatementAr: ['', Validators.required],
    proposedSolutionAr: ['', Validators.required],
    expectedBenefitsAr: ['', Validators.required],
    strategicThemeId: ['', Validators.required],
    activityId: ['', Validators.required],
    challengeId: [''],
    participationType: ['individual' as 'individual' | 'team', Validators.required],
    teamName: [''],
  });

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async reload(): Promise<void> {
    await this.load();
  }

  /** A section is editable in draft mode always, and when returned only if the supervisor unlocked it. */
  sectionEditable(key: string): boolean {
    return !this.isReturned() || this.allowedSections().has(key);
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      this.themes.set(await this.themesApi.list());
      this.activities.set(await this.activitiesApi.list());

      const id = this.route.snapshot.paramMap.get('id');
      if (id) {
        this.ideaId = id;
        const idea = await this.ideasApi.getById(id);
        this.loadedIdea = idea;
        this.existingAttachments.set(idea.attachments ?? []);
        if (idea.strategicThemeId) {
          this.challengeOptions.set(await this.challengesApi.listByTheme(idea.strategicThemeId));
        }
        this.form.patchValue({
          titleAr: idea.titleAr,
          problemStatementAr: idea.problemStatementAr,
          proposedSolutionAr: idea.proposedSolutionAr,
          expectedBenefitsAr: idea.expectedBenefitsAr,
          strategicThemeId: idea.strategicThemeId,
          activityId: idea.activityId,
          challengeId: idea.challengeId ?? '',
          participationType: idea.participationType,
          teamName: idea.teamName ?? '',
        });
        this.teamMembers.set([...idea.teamMembers]);
        this.initialTeam.set(
          idea.teamMembers.map((m) => ({ samAccountName: m.samAccountName, displayName: m.name, email: m.email })),
        );
        this.applySectionLocking(idea);
      }
    } catch {
      this.loadError.set($localize`:@@ideaFormLoadError:Couldn't load the idea. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * When an idea is returned, disables every control whose section the supervisor didn't unlock
   * (a null/empty `editableSections` means all sections are editable, matching the backend). Problem
   * statement and expected benefits are always read-only while returned — they aren't resubmittable.
   */
  private applySectionLocking(idea: Idea): void {
    const returned = idea.status === 'returned';
    this.isReturned.set(returned);

    if (!returned) {
      this.allowedSections.set(new Set());
      this.form.enable();
      return;
    }

    const sections = (idea.editableSections ?? '')
      .split(',')
      .map((s) => s.trim())
      .filter((s) => s.length > 0);
    const allowed = new Set(sections.length > 0 ? sections : ALL_SECTIONS);
    this.allowedSections.set(allowed);

    for (const [control, key] of Object.entries(SECTION_KEY_BY_CONTROL)) {
      if (allowed.has(key)) this.form.get(control)!.enable();
      else this.form.get(control)!.disable();
    }
  }

  async onThemeSelected(): Promise<void> {
    const themeId = this.form.controls.strategicThemeId.value;
    this.form.controls.challengeId.setValue('');
    this.challengeOptions.set(themeId ? await this.challengesApi.listByTheme(themeId) : []);
  }

  onTeamMembersSelected(people: DirectoryPerson | DirectoryPerson[]): void {
    const list = Array.isArray(people) ? people : [people];
    this.teamMembers.set(list.map((p) => ({ samAccountName: p.samAccountName, name: p.displayName, email: p.email })));
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
    if (!this.ideaId || !this.loadedIdea) return;
    this.errorMessage.set(null);

    // getRawValue() includes disabled (locked) controls, so unchanged sections carry their original
    // values through — the backend compares them against the stored idea and accepts the no-op.
    const raw = this.form.getRawValue();
    const isTeam = raw.participationType === 'team';

    try {
      if (this.isReturned()) {
        // Uploads must happen while the idea is still "returned"; resubmit then flips it to submitted.
        if (this.sectionEditable('attachments')) {
          await this.uploadQueuedFiles(this.ideaId);
        }
        const input: IdeaResubmitInput = {
          titleAr: raw.titleAr,
          titleEn: raw.titleAr,
          problemStatementAr: raw.problemStatementAr,
          problemStatementEn: raw.problemStatementAr,
          proposedSolutionAr: raw.proposedSolutionAr,
          proposedSolutionEn: raw.proposedSolutionAr,
          expectedBenefitsAr: raw.expectedBenefitsAr,
          expectedBenefitsEn: raw.expectedBenefitsAr,
          activityId: raw.activityId,
          strategicThemeId: raw.strategicThemeId,
          challengeId: raw.challengeId || null,
          participationType: raw.participationType,
          teamName: isTeam ? raw.teamName || null : null,
          teamMembers: isTeam ? this.teamMembers() : [],
        };
        await this.ideasApi.resubmit(this.ideaId, input);
      } else {
        const input: IdeaInput = {
          titleAr: raw.titleAr,
          titleEn: raw.titleAr,
          problemStatementAr: raw.problemStatementAr,
          problemStatementEn: raw.problemStatementAr,
          proposedSolutionAr: raw.proposedSolutionAr,
          proposedSolutionEn: raw.proposedSolutionAr,
          expectedBenefitsAr: raw.expectedBenefitsAr,
          expectedBenefitsEn: raw.expectedBenefitsAr,
          strategicThemeId: raw.strategicThemeId,
          activityId: raw.activityId,
          challengeId: raw.challengeId || null,
          participationType: raw.participationType,
          teamName: isTeam ? raw.teamName || null : null,
          teamMembers: isTeam ? this.teamMembers() : [],
          ipAcknowledged: this.loadedIdea.ipAcknowledged,
          termsAgreed: this.loadedIdea.termsAgreed,
        };
        await this.ideasApi.update(this.ideaId, input);
        await this.uploadQueuedFiles(this.ideaId);
      }
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
