import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { RoleCatalogPatch, RoleCatalogRow } from './roles-catalog.model';

@Injectable({ providedIn: 'root' })
export class RolesCatalogApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<RoleCatalogRow[]> {
    return firstValueFrom(this.http.get<RoleCatalogRow[]>('/api/admin/roles'));
  }

  patch(id: string, patch: RoleCatalogPatch): Promise<RoleCatalogRow> {
    return firstValueFrom(this.http.patch<RoleCatalogRow>(`/api/admin/roles/${id}`, patch));
  }
}
