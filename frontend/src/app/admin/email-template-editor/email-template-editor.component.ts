import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EmailTemplateApiService } from '../email-template-api.service';
import { EmailTemplate, EmailTemplateAttachment } from '../email-template.model';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

const KIND_ORDER = ['invite', 'accept', 'reject', 'reminder'];

@Component({
  selector: 'app-email-template-editor',
  imports: [FormsModule, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './email-template-editor.component.html',
})
export class EmailTemplateEditorComponent implements OnInit {
  private readonly api = inject(EmailTemplateApiService);

  readonly templates = signal<EmailTemplate[]>([]);
  readonly attachments = signal<EmailTemplateAttachment[]>([]);
  readonly activeKind = signal<string>('invite');
  readonly subjectAr = signal('');
  readonly subjectEn = signal('');
  readonly bodyAr = signal('');
  readonly bodyEn = signal('');
  readonly isBroadcast = signal(false);
  readonly saving = signal(false);
  readonly uploading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

  readonly kindOrder = KIND_ORDER;

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
      const templates = await this.api.list();
      this.templates.set(templates);
      await this.switchTo(templates[0]?.kind ?? 'invite');
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@emailTemplateLoadError:Couldn't load email templates. Please try again.`),
      );
    } finally {
      this.loading.set(false);
    }
  }

  private get currentTemplate(): EmailTemplate | undefined {
    return this.templates().find((t) => t.kind === this.activeKind());
  }

  async switchTo(kind: string): Promise<void> {
    this.activeKind.set(kind);
    const template = this.templates().find((t) => t.kind === kind);
    this.subjectAr.set(template?.subjectAr ?? '');
    this.subjectEn.set(template?.subjectEn ?? '');
    this.bodyAr.set(template?.bodyAr ?? '');
    this.bodyEn.set(template?.bodyEn ?? '');
    this.isBroadcast.set(template?.isBroadcast ?? false);
    this.errorMessage.set(null);
    if (template) {
      this.attachments.set(await this.api.listAttachments(template.id));
    }
  }

  async save(): Promise<void> {
    const template = this.currentTemplate;
    if (!template) return;
    this.saving.set(true);
    this.errorMessage.set(null);
    try {
      const updated = await this.api.update(template.id, {
        subjectAr: this.subjectAr(),
        subjectEn: this.subjectEn(),
        bodyAr: this.bodyAr(),
        bodyEn: this.bodyEn(),
        isBroadcast: this.isBroadcast(),
      });
      this.templates.update((list) => list.map((t) => (t.id === updated.id ? updated : t)));
      this.subjectEn.set(updated.subjectEn);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  async onFileSelected(file: File): Promise<void> {
    const template = this.currentTemplate;
    if (!template) return;
    this.uploading.set(true);
    this.errorMessage.set(null);
    try {
      await this.api.uploadAttachment(template.id, file);
      this.attachments.set(await this.api.listAttachments(template.id));
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.uploading.set(false);
    }
  }

  async onDeleteAttachment(id: string): Promise<void> {
    const template = this.currentTemplate;
    if (!template) return;
    this.errorMessage.set(null);
    try {
      await this.api.deleteAttachment(id);
      this.attachments.set(await this.api.listAttachments(template.id));
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
