import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { HomeBuilderApiService } from './home-builder-api.service';
import { LocPairInputComponent } from './loc-pair-input.component';
import { MediaFieldInputComponent } from './media-field-input.component';
import { RepeatableListComponent } from './repeatable-list.component';
import {
  AdminHomeSection,
  FieldDef,
  HOME_SECTION_TYPES,
  HomeSectionContent,
  HomeSectionInput,
  Loc,
  SECTION_FIELD_DEFS,
  emptyLoc,
  emptyObjArrayRow,
  emptySectionContent,
} from './home-builder.model';

/** A section row as edited locally in the builder. `key` is a client-only stable identity
 * (independent of the server `id`, which is null until the row has been persisted). */
export interface HomeBuilderRow {
  key: string;
  id: string | null;
  type: string;
  isVisible: boolean;
  content: HomeSectionContent;
}

let rowKeySeq = 0;
function nextKey(): string {
  rowKeySeq += 1;
  return `row-${rowKeySeq}`;
}

@Component({
  selector: 'app-home-builder',
  imports: [
    FormsModule,
    PageHeaderComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    LocPairInputComponent,
    MediaFieldInputComponent,
    RepeatableListComponent,
  ],
  templateUrl: './home-builder.component.html',
})
export class HomeBuilderComponent implements OnInit {
  private readonly api = inject(HomeBuilderApiService);

  readonly rows = signal<HomeBuilderRow[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly expandedKeys = signal<ReadonlySet<string>>(new Set());
  readonly newSectionType = signal<string>(HOME_SECTION_TYPES[0]);

  readonly sectionTypes = HOME_SECTION_TYPES;

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
      const sections = await this.api.listSections();
      this.rows.set(sections.map((s) => this.toRow(s)));
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(
          error,
          $localize`:@@homeBuilderLoadError:Couldn't load homepage sections. Please try again.`,
        ),
      );
    } finally {
      this.loading.set(false);
    }
  }

  private toRow(s: AdminHomeSection): HomeBuilderRow {
    return { key: nextKey(), id: s.id, type: s.type, isVisible: s.isVisible, content: s.contentJson ?? {} };
  }

  fieldsFor(type: string): FieldDef[] {
    return SECTION_FIELD_DEFS[type] ?? [];
  }

  isExpanded(key: string): boolean {
    return this.expandedKeys().has(key);
  }

  toggleExpanded(key: string): void {
    this.expandedKeys.update((set) => {
      const next = new Set(set);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  }

  toggleVisible(key: string): void {
    this.rows.update((list) => list.map((r) => (r.key === key ? { ...r, isVisible: !r.isVisible } : r)));
    this.saved.set(false);
  }

  moveUp(key: string): void {
    this.rows.update((list) => {
      const i = list.findIndex((r) => r.key === key);
      if (i <= 0) return list;
      const next = [...list];
      [next[i - 1], next[i]] = [next[i], next[i - 1]];
      return next;
    });
    this.saved.set(false);
  }

  moveDown(key: string): void {
    this.rows.update((list) => {
      const i = list.findIndex((r) => r.key === key);
      if (i === -1 || i >= list.length - 1) return list;
      const next = [...list];
      [next[i + 1], next[i]] = [next[i], next[i + 1]];
      return next;
    });
    this.saved.set(false);
  }

  setLocField(key: string, fieldKey: string, value: Loc): void {
    this.updateContent(key, (content) => ({ ...content, [fieldKey]: value }));
  }

  setTextField(key: string, fieldKey: string, value: string): void {
    this.updateContent(key, (content) => ({ ...content, [fieldKey]: value }));
  }

  addArrayRow(key: string, fieldKey: string, emptyRow: unknown): void {
    this.updateContent(key, (content) => {
      const arr = Array.isArray(content[fieldKey]) ? (content[fieldKey] as unknown[]) : [];
      return { ...content, [fieldKey]: [...arr, emptyRow] };
    });
  }

  removeArrayRow(key: string, fieldKey: string, index: number): void {
    this.updateContent(key, (content) => {
      const arr = Array.isArray(content[fieldKey]) ? (content[fieldKey] as unknown[]) : [];
      return { ...content, [fieldKey]: arr.filter((_, i) => i !== index) };
    });
  }

  setLocArrayItem(key: string, fieldKey: string, index: number, value: Loc): void {
    this.updateContent(key, (content) => {
      const arr = Array.isArray(content[fieldKey]) ? [...(content[fieldKey] as Loc[])] : [];
      arr[index] = value;
      return { ...content, [fieldKey]: arr };
    });
  }

  setObjArrayItemField(key: string, fieldKey: string, index: number, subKey: string, value: unknown): void {
    this.updateContent(key, (content) => {
      const arr = Array.isArray(content[fieldKey]) ? [...(content[fieldKey] as Record<string, unknown>[])] : [];
      arr[index] = { ...(arr[index] ?? {}), [subKey]: value };
      return { ...content, [fieldKey]: arr };
    });
  }

  private updateContent(key: string, updater: (content: HomeSectionContent) => HomeSectionContent): void {
    this.rows.update((list) => list.map((r) => (r.key === key ? { ...r, content: updater(r.content) } : r)));
    this.saved.set(false);
  }

  async addSection(): Promise<void> {
    const type = this.newSectionType();
    this.errorMessage.set(null);
    const content = emptySectionContent(type);
    const input: HomeSectionInput = {
      id: null,
      idx: this.rows().length,
      type,
      isVisible: true,
      contentJson: JSON.stringify(content),
    };
    try {
      const created = await this.api.addSection(input);
      this.rows.update((list) => [...list, this.toRow(created)]);
      this.saved.set(false);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async deleteRow(key: string): Promise<void> {
    const row = this.rows().find((r) => r.key === key);
    if (!row) return;
    this.errorMessage.set(null);
    try {
      if (row.id) {
        await this.api.deleteSection(row.id);
      }
      this.rows.update((list) => list.filter((r) => r.key !== key));
      this.saved.set(false);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async save(): Promise<void> {
    this.saving.set(true);
    this.errorMessage.set(null);
    this.saved.set(false);
    try {
      const inputs: HomeSectionInput[] = this.rows().map((r, idx) => ({
        id: r.id,
        idx,
        type: r.type,
        isVisible: r.isVisible,
        contentJson: JSON.stringify(r.content),
      }));
      const result = await this.api.saveSections(inputs);
      this.rows.set(result.map((s) => this.toRow(s)));
      this.saved.set(true);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  getLoc(row: HomeBuilderRow, key: string): Loc {
    return this.asLoc(row.content[key]);
  }

  getText(row: HomeBuilderRow, key: string): string {
    const v = row.content[key];
    return typeof v === 'string' ? v : '';
  }

  getArray(row: HomeBuilderRow, key: string): unknown[] {
    const v = row.content[key];
    return Array.isArray(v) ? v : [];
  }

  getSubLoc(item: unknown, key: string): Loc {
    return this.asLoc((item as Record<string, unknown> | null)?.[key]);
  }

  getSubText(item: unknown, key: string): string {
    const v = (item as Record<string, unknown> | null)?.[key];
    return typeof v === 'string' ? v : '';
  }

  getSubNumber(item: unknown, key: string): number {
    const v = (item as Record<string, unknown> | null)?.[key];
    return typeof v === 'number' ? v : 0;
  }

  emptyRowFor(field: FieldDef): unknown {
    if (field.kind === 'locArray') return emptyLoc();
    if (field.kind === 'objArray') return emptyObjArrayRow(field.objFields ?? []);
    return '';
  }

  private asLoc(v: unknown): Loc {
    if (v && typeof v === 'object' && 'ar' in v && 'en' in v) {
      return v as Loc;
    }
    return emptyLoc();
  }

  private extractErrorMessage(error: unknown, fallback = $localize`Something went wrong. Please try again.`): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return fallback;
  }
}
