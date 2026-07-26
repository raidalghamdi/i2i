import { Component, OnInit, computed, inject, signal } from '@angular/core'; // Change 20260726
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms'; // Change 20260726
import { ActivatedRoute, Router, RouterLink } from '@angular/router'; // Change 20260726
import { PageHeaderComponent } from '../../shared/page-header/page-header.component'; // Change 20260726
import { IdeaAttachment, StrategicTheme } from '../idea.model'; // Change 20260726
import { IdeaDetailView, IdeaDraftInput, IdeasService } from '../ideas.service'; // Change 20260726
import { backendError } from '../shared/withdraw-dialog.component'; // Change 20260726

/** Statuses whose owner may still edit the idea. */ // Change 20260726
const EDITABLE_STATUSES = new Set(['draft', 'needs_completion', 'returned']); // Change 20260726

/** Form control names, in the order they appear, used to apply `editableSections`. */ // Change 20260726
const SECTIONS = ['titleEn', 'titleAr', 'descriptionEn', 'descriptionAr', 'strategicThemeId'] as const; // Change 20260726

type Section = (typeof SECTIONS)[number]; // Change 20260726

/** // Change 20260726
 * `editableSections` arrives as a JSON array of section keys. Anything unparseable // Change 20260726
 * is treated as "no restriction" rather than locking the whole form. // Change 20260726
 */ // Change 20260726
export function parseEditableSections(raw: string | null): string[] { // Change 20260726
  if (!raw?.trim()) return []; // Change 20260726
  try { // Change 20260726
    const parsed: unknown = JSON.parse(raw); // Change 20260726
    return Array.isArray(parsed) ? parsed.filter((s): s is string => typeof s === 'string') : []; // Change 20260726
  } catch { // Change 20260726
    return []; // Change 20260726
  } // Change 20260726
} // Change 20260726

@Component({ // Change 20260726
  selector: 'app-idea-edit', // Change 20260726
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent], // Change 20260726
  templateUrl: './idea-edit.component.html', // Change 20260726
  styleUrl: './idea-edit.component.scss', // Change 20260726
}) // Change 20260726
export class IdeaEditComponent implements OnInit { // Change 20260726
  private readonly ideas = inject(IdeasService); // Change 20260726
  private readonly router = inject(Router); // Change 20260726
  private readonly route = inject(ActivatedRoute); // Change 20260726

  readonly form = inject(FormBuilder).nonNullable.group({ // Change 20260726
    titleEn: ['', Validators.required], // Change 20260726
    titleAr: [''], // Change 20260726
    descriptionEn: [''], // Change 20260726
    descriptionAr: [''], // Change 20260726
    strategicThemeId: [''], // Change 20260726
  }); // Change 20260726

  readonly ideaId = signal(''); // Change 20260726
  readonly idea = signal<IdeaDetailView | null>(null); // Change 20260726
  readonly themes = signal<StrategicTheme[]>([]); // Change 20260726
  readonly themesFailed = signal(false); // Change 20260726
  readonly existingAttachments = signal<IdeaAttachment[]>([]); // Change 20260726
  readonly loading = signal(true); // Change 20260726
  readonly saving = signal(false); // Change 20260726
  readonly error = signal<string | null>(null); // Change 20260726
  /** Set when the idea cannot be edited at all — renders the blocked page instead of the form. */ // Change 20260726
  readonly blocked = signal<string | null>(null); // Change 20260726
  readonly confirming = signal(false); // Change 20260726
  readonly submitAttempted = signal(false); // Change 20260726
  readonly restrictedSections = signal<string[]>([]); // Change 20260726

  private readonly newFiles: File[] = []; // Change 20260726
  readonly pendingUploads = signal<string[]>([]); // Change 20260726

  readonly status = computed(() => this.idea()?.status?.toLowerCase() ?? ''); // Change 20260726
  readonly canSaveDraft = computed(() => this.status() === 'draft'); // Change 20260726
  readonly isRestricted = computed(() => this.restrictedSections().length > 0); // Change 20260726

  ngOnInit(): void { // Change 20260726
    const id = this.route.snapshot.paramMap.get('id') ?? ''; // Change 20260726
    this.ideaId.set(id); // Change 20260726
    void this.load(id); // Change 20260726
  } // Change 20260726

  async load(id: string): Promise<void> { // Change 20260726
    this.loading.set(true); // Change 20260726
    try { // Change 20260726
      const idea = await this.ideas.getById(id); // Change 20260726
      this.idea.set(idea); // Change 20260726

      if (!EDITABLE_STATUSES.has(idea.status?.toLowerCase())) { // Change 20260726
        this.blocked.set( // Change 20260726
          $localize`:@@ideas.edit.blockedStatus:This idea can no longer be edited at its current stage.`, // Change 20260726
        ); // Change 20260726
        return; // Change 20260726
      } // Change 20260726

      this.form.patchValue({ // Change 20260726
        titleEn: idea.titleEn ?? '', // Change 20260726
        titleAr: idea.titleAr ?? '', // Change 20260726
        descriptionEn: idea.problemStatementEn ?? '', // Change 20260726
        descriptionAr: idea.problemStatementAr ?? '', // Change 20260726
        strategicThemeId: idea.strategicThemeId ?? '', // Change 20260726
      }); // Change 20260726
      this.existingAttachments.set(idea.attachments ?? []); // Change 20260726
      this.applyEditableSections(idea); // Change 20260726
      await this.loadThemes(); // Change 20260726
    } catch (err: unknown) { // Change 20260726
      // A non-owner gets 403 and a bad id gets 404; both mean "not yours to edit". // Change 20260726
      this.blocked.set( // Change 20260726
        (err as { status?: number })?.status === 403 // Change 20260726
          ? $localize`:@@ideas.edit.blockedOwner:You can only edit ideas you submitted.` // Change 20260726
          : $localize`:@@ideas.edit.blockedMissing:This idea could not be loaded.`, // Change 20260726
      ); // Change 20260726
    } finally { // Change 20260726
      this.loading.set(false); // Change 20260726
    } // Change 20260726
  } // Change 20260726

  /** Disables every control not named in `editableSections` while `needs_completion`. */ // Change 20260726
  private applyEditableSections(idea: IdeaDetailView): void { // Change 20260726
    if (idea.status?.toLowerCase() !== 'needs_completion') return; // Change 20260726
    const allowed = parseEditableSections(idea.editableSections); // Change 20260726
    if (allowed.length === 0) return; // Change 20260726

    this.restrictedSections.set(allowed); // Change 20260726
    for (const section of SECTIONS) { // Change 20260726
      if (!allowed.includes(section)) this.form.controls[section as Section].disable(); // Change 20260726
    } // Change 20260726
  } // Change 20260726

  async loadThemes(): Promise<void> { // Change 20260726
    try { // Change 20260726
      this.themes.set(await this.ideas.getStrategicThemes()); // Change 20260726
      this.themesFailed.set(false); // Change 20260726
    } catch { // Change 20260726
      this.themes.set([]); // Change 20260726
      this.themesFailed.set(true); // Change 20260726
    } // Change 20260726
  } // Change 20260726

  themeLabel(theme: StrategicTheme): string { // Change 20260726
    return theme.nameEn?.trim() || theme.nameAr?.trim() || theme.id; // Change 20260726
  } // Change 20260726

  isEditableSection(section: string): boolean { // Change 20260726
    return !this.isRestricted() || this.restrictedSections().includes(section); // Change 20260726
  } // Change 20260726

  onFilesSelected(event: Event): void { // Change 20260726
    const input = event.target as HTMLInputElement; // Change 20260726
    const picked = Array.from(input.files ?? []); // Change 20260726
    this.newFiles.push(...picked); // Change 20260726
    this.pendingUploads.update((current) => [...current, ...picked.map((f) => f.name)]); // Change 20260726
    input.value = ''; // Change 20260726
  } // Change 20260726

  removePendingUpload(index: number): void { // Change 20260726
    this.newFiles.splice(index, 1); // Change 20260726
    this.pendingUploads.update((current) => current.filter((_, i) => i !== index)); // Change 20260726
  } // Change 20260726

  async deleteExistingAttachment(attachment: IdeaAttachment): Promise<void> { // Change 20260726
    this.error.set(null); // Change 20260726
    try { // Change 20260726
      await this.ideas.deleteAttachment(this.ideaId(), attachment.id); // Change 20260726
      this.existingAttachments.update((current) => current.filter((a) => a.id !== attachment.id)); // Change 20260726
    } catch (err: unknown) { // Change 20260726
      this.error.set(backendError(err)); // Change 20260726
    } // Change 20260726
  } // Change 20260726

  isSubmitReady(): boolean { // Change 20260726
    const value = this.form.getRawValue(); // Change 20260726
    return ( // Change 20260726
      !!value.titleEn.trim() && // Change 20260726
      !!value.titleAr.trim() && // Change 20260726
      !!value.descriptionEn.trim() && // Change 20260726
      !!value.descriptionAr.trim() && // Change 20260726
      !!value.strategicThemeId // Change 20260726
    ); // Change 20260726
  } // Change 20260726

  async saveDraft(): Promise<void> { // Change 20260726
    this.submitAttempted.set(false); // Change 20260726
    if (this.form.controls.titleEn.invalid) { // Change 20260726
      this.form.controls.titleEn.markAsTouched(); // Change 20260726
      return; // Change 20260726
    } // Change 20260726
    await this.persist(false); // Change 20260726
  } // Change 20260726

  requestResubmit(): void { // Change 20260726
    this.submitAttempted.set(true); // Change 20260726
    if (!this.isSubmitReady()) { // Change 20260726
      this.form.markAllAsTouched(); // Change 20260726
      return; // Change 20260726
    } // Change 20260726
    this.confirming.set(true); // Change 20260726
  } // Change 20260726

  cancelResubmit(): void { // Change 20260726
    this.confirming.set(false); // Change 20260726
  } // Change 20260726

  async confirmResubmit(): Promise<void> { // Change 20260726
    this.confirming.set(false); // Change 20260726
    await this.persist(true); // Change 20260726
  } // Change 20260726

  private async persist(submit: boolean): Promise<void> { // Change 20260726
    this.saving.set(true); // Change 20260726
    this.error.set(null); // Change 20260726
    try { // Change 20260726
      const id = this.ideaId(); // Change 20260726
      // getRawValue includes controls disabled by the editableSections rule, so // Change 20260726
      // locked fields round-trip unchanged instead of being cleared. // Change 20260726
      await this.ideas.updateIdea(id, this.form.getRawValue() as IdeaDraftInput); // Change 20260726
      for (const file of this.newFiles) await this.ideas.addAttachment(id, file); // Change 20260726
      if (submit) await this.ideas.submitIdea(id); // Change 20260726
      await this.router.navigate(['/ideas/mine']); // Change 20260726
    } catch (err: unknown) { // Change 20260726
      this.error.set(backendError(err)); // Change 20260726
    } finally { // Change 20260726
      this.saving.set(false); // Change 20260726
    } // Change 20260726
  } // Change 20260726
} // Change 20260726
