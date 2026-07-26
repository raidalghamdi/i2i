import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { EmailTemplate, EmailTemplateAttachment, EmailTemplateUpdateInput } from './email-template.model';

@Injectable({ providedIn: 'root' })
export class EmailTemplateApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<EmailTemplate[]> {
    return firstValueFrom(this.http.get<EmailTemplate[]>('/api/admin/email-templates'));
  }

  update(id: string, input: EmailTemplateUpdateInput): Promise<EmailTemplate> {
    return firstValueFrom(this.http.patch<EmailTemplate>(`/api/admin/email-templates/${id}`, input));
  }

  listAttachments(templateId: string): Promise<EmailTemplateAttachment[]> {
    return firstValueFrom(this.http.get<EmailTemplateAttachment[]>(`/api/admin/email-templates/${templateId}/attachments`));
  }

  uploadAttachment(templateId: string, file: File): Promise<EmailTemplateAttachment> {
    const formData = new FormData();
    formData.append('file', file);
    return firstValueFrom(this.http.post<EmailTemplateAttachment>(`/api/admin/email-templates/${templateId}/attachments`, formData));
  }

  deleteAttachment(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/admin/email-template-attachments/${id}`));
  }
}
