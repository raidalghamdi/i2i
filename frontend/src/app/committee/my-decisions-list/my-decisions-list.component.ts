import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { CommitteeApiService } from '../committee-api.service';
import { CommitteeDecisionAttachment, MyCommitteeDecision } from '../committee.model';

// Change 20260726 — leaves the blob URL alive long enough for the new tab to load it.
const ATTACHMENT_URL_REVOKE_MS = 60_000;

@Component({
  selector: 'app-my-decisions-list',
  imports: [DatePipe, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './my-decisions-list.component.html',
})
export class MyDecisionsListComponent implements OnInit {
  private readonly committeeApi = inject(CommitteeApiService);
  readonly decisions = signal<MyCommitteeDecision[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly attachmentError = signal<string | null>(null); // Change 20260726

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  reload(): void {
    void this.load();
  }

  // Change 20260726 — the endpoint needs the auth headers only HttpClient adds, so the file is
  // fetched as a blob and handed to the browser rather than linked to directly.
  async openAttachment(decisionId: string, attachment: CommitteeDecisionAttachment): Promise<void> {
    this.attachmentError.set(null);
    // Opened synchronously inside the click gesture so popup blockers allow it.
    const win = window.open('', '_blank');
    try {
      const blob = await this.committeeApi.getAttachment(decisionId, attachment.id);
      const url = URL.createObjectURL(blob);
      if (win) {
        win.location.href = url;
      } else {
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = attachment.fileName;
        anchor.click();
      }
      setTimeout(() => URL.revokeObjectURL(url), ATTACHMENT_URL_REVOKE_MS);
    } catch {
      win?.close();
      this.attachmentError.set($localize`:@@myDecisionsAttachmentError:Couldn't download the attachment. Please try again.`);
    }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.decisions.set(await this.committeeApi.getMine());
    } catch {
      this.error.set($localize`:@@myDecisionsLoadError:Couldn't load your committee decisions. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }
}
