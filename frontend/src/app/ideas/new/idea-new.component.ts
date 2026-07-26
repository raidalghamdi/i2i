import { Component, OnInit, inject, signal } from '@angular/core'; // Change 20260726
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms'; // Change 20260726
import { Router } from '@angular/router'; // Change 20260726
import { PageHeaderComponent } from '../../shared/page-header/page-header.component'; // Change 20260726
import { StrategicTheme } from '../idea.model'; // Change 20260726
import { IdeaDraftInput, IdeasService, PendingAttachment } from '../ideas.service'; // Change 20260726
import { backendError } from '../shared/withdraw-dialog.component'; // Change 20260726

/** Single-page draft/submit form for a new idea. */ // Change 20260726
@Component({ // Change 20260726
  selector: 'app-idea-new', // Change 20260726
  imports: [ReactiveFormsModule, PageHeaderComponent], // Change 20260726
  templateUrl: './idea-new.component.html', // Change 20260726
  styleUrl: './idea-new.component.scss', // Change 20260726
}) // Change 20260726
export class IdeaNewComponent implements OnInit { // Change 20260726
  private readonly ideas = inject(IdeasService); // Change 20260726
  private readonly router = inject(Router); // Change 20260726

  readonly form = inject(FormBuilder).nonNullable.group({ // Change 20260726
    // Only Title EN is structurally required — a draft may leave the rest blank. // Change 20260726
    titleEn: ['', Validators.required], // Change 20260726
    titleAr: [''], // Change 20260726
    descriptionEn: [''], // Change 20260726
    descriptionAr: [''], // Change 20260726
    strategicThemeId: [''], // Change 20260726
  }); // Change 20260726

  readonly themes = signal<StrategicTheme[]>([]); // Change 20260726
  readonly themesFailed = signal(false); // Change 20260726
  readonly attachments = signal<PendingAttachment[]>([]); // Change 20260726
  /** Parallel to `attachments`; the real files, uploaded once the idea has an id. */ // Change 20260726
  private readonly files: File[] = []; // Change 20260726
  readonly saving = signal(false); // Change 20260726
  readonly error = signal<string | null>(null); // Change 20260726
  readonly confirming = signal(false); // Change 20260726
  /** Set once Submit is attempted, so validation messages stay hidden while drafting. */ // Change 20260726
  readonly submitAttempted = signal(false); // Change 20260726

  ngOnInit(): void { // Change 20260726
    void this.loadThemes(); // Change 20260726
  } // Change 20260726

  async loadThemes(): Promise<void> { // Change 20260726
    try { // Change 20260726
      this.themes.set(await this.ideas.getStrategicThemes()); // Change 20260726
      this.themesFailed.set(false); // Change 20260726
    } catch { // Change 20260726
      // A missing theme list must not block saving a draft. // Change 20260726
      this.themes.set([]); // Change 20260726
      this.themesFailed.set(true); // Change 20260726
    } // Change 20260726
  } // Change 20260726

  themeLabel(theme: StrategicTheme): string { // Change 20260726
    return theme.nameEn?.trim() || theme.nameAr?.trim() || theme.id; // Change 20260726
  } // Change 20260726

  onFilesSelected(event: Event): void { // Change 20260726
    const input = event.target as HTMLInputElement; // Change 20260726
    const picked = Array.from(input.files ?? []); // Change 20260726
    this.attachments.update((current) => [ // Change 20260726
      ...current, // Change 20260726
      ...picked.map((file) => ({ // Change 20260726
        filename: file.name, // Change 20260726
        sizeBytes: file.size, // Change 20260726
        mimeType: file.type, // Change 20260726
        url: null, // Change 20260726
      })), // Change 20260726
    ]); // Change 20260726
    this.files.push(...picked); // Change 20260726
    // Clear the control so re-picking the same file fires another change event. // Change 20260726
    input.value = ''; // Change 20260726
  } // Change 20260726

  removeAttachment(index: number): void { // Change 20260726
    this.attachments.update((current) => current.filter((_, i) => i !== index)); // Change 20260726
    this.files.splice(index, 1); // Change 20260726
  } // Change 20260726

  /** True when every field the backend needs for a submit is filled in. */ // Change 20260726
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

  requestSubmit(): void { // Change 20260726
    this.submitAttempted.set(true); // Change 20260726
    if (!this.isSubmitReady()) { // Change 20260726
      this.form.markAllAsTouched(); // Change 20260726
      return; // Change 20260726
    } // Change 20260726
    this.confirming.set(true); // Change 20260726
  } // Change 20260726

  cancelSubmit(): void { // Change 20260726
    this.confirming.set(false); // Change 20260726
  } // Change 20260726

  async confirmSubmit(): Promise<void> { // Change 20260726
    this.confirming.set(false); // Change 20260726
    await this.persist(true); // Change 20260726
  } // Change 20260726

  private async persist(submit: boolean): Promise<void> { // Change 20260726
    this.saving.set(true); // Change 20260726
    this.error.set(null); // Change 20260726
    try { // Change 20260726
      // There is no combined create-and-submit endpoint, so the draft is created // Change 20260726
      // first and then transitioned. // Change 20260726
      const created = await this.ideas.createDraft(this.form.getRawValue() as IdeaDraftInput); // Change 20260726
      for (const file of this.files) await this.ideas.addAttachment(created.id, file); // Change 20260726
      if (submit) await this.ideas.submitIdea(created.id); // Change 20260726
      await this.router.navigate(['/ideas/mine']); // Change 20260726
    } catch (err: unknown) { // Change 20260726
      this.error.set(backendError(err)); // Change 20260726
    } finally { // Change 20260726
      this.saving.set(false); // Change 20260726
    } // Change 20260726
  } // Change 20260726
} // Change 20260726
