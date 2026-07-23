import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  CmsBlock,
  CmsBlockInput,
  CmsContent,
  CmsContentInput,
  ContentString,
  ContentStringInput,
} from './cms.model';

@Injectable({ providedIn: 'root' })
export class CmsApiService {
  private readonly http = inject(HttpClient);

  listBlocks(): Promise<CmsBlock[]> {
    return firstValueFrom(this.http.get<CmsBlock[]>('/api/admin/cms/blocks'));
  }

  getBlock(id: string): Promise<CmsBlock> {
    return firstValueFrom(this.http.get<CmsBlock>(`/api/admin/cms/blocks/${id}`));
  }

  createBlock(input: CmsBlockInput): Promise<CmsBlock> {
    return firstValueFrom(this.http.post<CmsBlock>('/api/admin/cms/blocks', input));
  }

  updateBlock(id: string, input: CmsBlockInput): Promise<CmsBlock> {
    return firstValueFrom(this.http.put<CmsBlock>(`/api/admin/cms/blocks/${id}`, input));
  }

  deleteBlock(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/admin/cms/blocks/${id}`));
  }

  listContent(): Promise<CmsContent[]> {
    return firstValueFrom(this.http.get<CmsContent[]>('/api/admin/cms/content'));
  }

  getContent(id: string): Promise<CmsContent> {
    return firstValueFrom(this.http.get<CmsContent>(`/api/admin/cms/content/${id}`));
  }

  createContent(input: CmsContentInput): Promise<CmsContent> {
    return firstValueFrom(this.http.post<CmsContent>('/api/admin/cms/content', input));
  }

  updateContent(id: string, input: CmsContentInput): Promise<CmsContent> {
    return firstValueFrom(this.http.put<CmsContent>(`/api/admin/cms/content/${id}`, input));
  }

  deleteContent(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/admin/cms/content/${id}`));
  }

  listStrings(): Promise<ContentString[]> {
    return firstValueFrom(this.http.get<ContentString[]>('/api/admin/cms/strings'));
  }

  getString(id: string): Promise<ContentString> {
    return firstValueFrom(this.http.get<ContentString>(`/api/admin/cms/strings/${id}`));
  }

  createString(input: ContentStringInput): Promise<ContentString> {
    return firstValueFrom(this.http.post<ContentString>('/api/admin/cms/strings', input));
  }

  updateString(id: string, input: ContentStringInput): Promise<ContentString> {
    return firstValueFrom(this.http.put<ContentString>(`/api/admin/cms/strings/${id}`, input));
  }

  deleteString(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/admin/cms/strings/${id}`));
  }
}
