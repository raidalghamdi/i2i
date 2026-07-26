import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core'; // Change 20260726
import { FormsModule } from '@angular/forms'; // Change 20260726
import { IdeasApiService } from '../ideas-api.service'; // Change 20260726

/** Confirmation overlay for POST /api/ideas/:id/withdraw, with an optional reason. */ // Change 20260726
@Component({ // Change 20260726
  selector: 'app-withdraw-dialog', // Change 20260726
  imports: [FormsModule], // Change 20260726
  templateUrl: './withdraw-dialog.component.html', // Change 20260726
  styleUrl: './withdraw-dialog.component.scss', // Change 20260726
}) // Change 20260726
export class WithdrawDialogComponent { // Change 20260726
  private readonly ideasApi = inject(IdeasApiService); // Change 20260726

  @Input({ required: true }) ideaId!: string; // Change 20260726
  @Input() ideaTitle = ''; // Change 20260726

  @Output() readonly withdrawn = new EventEmitter<string>(); // Change 20260726
  @Output() readonly cancelled = new EventEmitter<void>(); // Change 20260726

  readonly reason = signal(''); // Change 20260726
  readonly saving = signal(false); // Change 20260726
  readonly error = signal<string | null>(null); // Change 20260726

  cancel(): void { // Change 20260726
    if (this.saving()) return; // Change 20260726
    this.cancelled.emit(); // Change 20260726
  } // Change 20260726

  async confirm(): Promise<void> { // Change 20260726
    if (this.saving()) return; // Change 20260726
    this.saving.set(true); // Change 20260726
    this.error.set(null); // Change 20260726
    try { // Change 20260726
      await this.ideasApi.withdraw(this.ideaId, this.reason().trim() || undefined); // Change 20260726
      this.withdrawn.emit(this.ideaId); // Change 20260726
    } catch (err: unknown) { // Change 20260726
      this.error.set(backendError(err)); // Change 20260726
    } finally { // Change 20260726
      this.saving.set(false); // Change 20260726
    } // Change 20260726
  } // Change 20260726
} // Change 20260726

/** Pulls the API's `{ error }` message out of a failed response, with a safe fallback. */ // Change 20260726
export function backendError(err: unknown): string { // Change 20260726
  const body = (err as { error?: unknown })?.error; // Change 20260726
  const message = (body as { error?: unknown })?.error; // Change 20260726
  return typeof message === 'string' && message.trim() // Change 20260726
    ? message // Change 20260726
    : $localize`:@@ideas.write.genericError:Something went wrong — please try again`; // Change 20260726
} // Change 20260726
