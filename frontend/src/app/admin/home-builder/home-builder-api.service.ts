import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AdminHomeSection, HomeSectionInput } from './home-builder.model';

@Injectable({ providedIn: 'root' })
export class HomeBuilderApiService {
  private readonly http = inject(HttpClient);

  listSections(): Promise<AdminHomeSection[]> {
    return firstValueFrom(this.http.get<AdminHomeSection[]>('/api/admin/home/sections'));
  }

  /** Replaces the whole section set. `contentJson` must already be JSON.stringify'd by the caller. */
  saveSections(sections: HomeSectionInput[]): Promise<AdminHomeSection[]> {
    return firstValueFrom(this.http.put<AdminHomeSection[]>('/api/admin/home/sections', { sections }));
  }

  addSection(input: HomeSectionInput): Promise<AdminHomeSection> {
    return firstValueFrom(this.http.post<AdminHomeSection>('/api/admin/home/sections', input));
  }

  deleteSection(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/admin/home/sections/${id}`));
  }

  /** Uploads a media file (image/video) for use in a homepage section's contentJson.
   * The returned `url` is a relative, same-origin path usable directly as an `<img>`/`<video>` src. */
  uploadMedia(file: File): Promise<{ id: string; url: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return firstValueFrom(this.http.post<{ id: string; url: string }>('/api/admin/home/media', formData));
  }
}
