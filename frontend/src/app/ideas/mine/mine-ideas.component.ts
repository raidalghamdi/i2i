import { DatePipe } from '@angular/common'; // Change 20260726
import { Component, OnInit, computed, inject, signal } from '@angular/core'; // Change 20260726
import { RouterLink } from '@angular/router'; // Change 20260726
import { PageHeaderComponent } from '../../shared/page-header/page-header.component'; // Change 20260726
import { StatusLabelPipe } from '../../shared/status-label/status-label.pipe'; // Change 20260726
import { IdeasService, MineIdeaRow } from '../ideas.service'; // Change 20260726

/** Statuses an innovator may still edit themselves. */ // Change 20260726
const EDITABLE_STATUSES = new Set(['draft', 'needs_completion', 'returned']); // Change 20260726

/** Pill colour groups for the whole idea lifecycle (Phase 2b spec). */ // Change 20260726
const STATUS_TONES: Record<string, string> = { // Change 20260726
  draft: 'bg-gray-100 text-gray-600', // Change 20260726
  needs_completion: 'bg-gray-100 text-gray-600', // Change 20260726
  submitted: 'bg-blue-50 text-blue-700', // Change 20260726
  screening: 'bg-blue-50 text-blue-700', // Change 20260726
  evaluation: 'bg-blue-50 text-blue-700', // Change 20260726
  committee: 'bg-blue-50 text-blue-700', // Change 20260726
  pending_final_ranking: 'bg-blue-50 text-blue-700', // Change 20260726
  approved: 'bg-emerald-50 text-emerald-700', // Change 20260726
  pass_awaiting_attachments: 'bg-emerald-50 text-emerald-700', // Change 20260726
  rejected: 'bg-red-50 text-red-700', // Change 20260726
  evaluation_failed: 'bg-red-50 text-red-700', // Change 20260726
  not_selected: 'bg-red-50 text-red-700', // Change 20260726
  in_pilot: 'bg-purple-50 text-purple-700', // Change 20260726
  in_implementation: 'bg-purple-50 text-purple-700', // Change 20260726
  benefits_tracking: 'bg-purple-50 text-purple-700', // Change 20260726
  in_measurement: 'bg-purple-50 text-purple-700', // Change 20260726
  in_scaling: 'bg-purple-50 text-purple-700', // Change 20260726
  withdrawn: 'bg-slate-100 text-slate-700', // Change 20260726
  archived: 'bg-slate-100 text-slate-700', // Change 20260726
  closed: 'bg-slate-100 text-slate-700', // Change 20260726
  returned: 'bg-amber-50 text-amber-800', // Change 20260726
}; // Change 20260726

export const MINE_IDEA_STATUSES: readonly string[] = Object.keys(STATUS_TONES); // Change 20260726

const PAGE_SIZE = 20; // Change 20260726

@Component({ // Change 20260726
  selector: 'app-mine-ideas', // Change 20260726
  imports: [DatePipe, RouterLink, PageHeaderComponent, StatusLabelPipe], // Change 20260726
  templateUrl: './mine-ideas.component.html', // Change 20260726
  styleUrl: './mine-ideas.component.scss', // Change 20260726
}) // Change 20260726
export class MineIdeasComponent implements OnInit { // Change 20260726
  private readonly ideas = inject(IdeasService); // Change 20260726

  readonly statuses = MINE_IDEA_STATUSES; // Change 20260726
  readonly items = signal<MineIdeaRow[]>([]); // Change 20260726
  readonly total = signal(0); // Change 20260726
  readonly page = signal(1); // Change 20260726
  readonly status = signal(''); // Change 20260726
  readonly sort = signal('createdAt desc'); // Change 20260726
  readonly loading = signal(false); // Change 20260726
  readonly error = signal<string | null>(null); // Change 20260726

  readonly totalPages = computed(() => Math.max(Math.ceil(this.total() / PAGE_SIZE), 1)); // Change 20260726
  // The API only sorts by date, so alphabetical-by-status is applied to the fetched page. // Change 20260726
  readonly rows = computed(() => // Change 20260726
    this.sort() === 'status' // Change 20260726
      ? [...this.items()].sort((a, b) => a.status.localeCompare(b.status)) // Change 20260726
      : this.items(), // Change 20260726
  ); // Change 20260726
  readonly skeletonRows = [0, 1, 2, 3, 4]; // Change 20260726

  ngOnInit(): void { // Change 20260726
    void this.reload(); // Change 20260726
  } // Change 20260726

  async reload(): Promise<void> { // Change 20260726
    this.loading.set(true); // Change 20260726
    this.error.set(null); // Change 20260726
    try { // Change 20260726
      const result = await this.ideas.getMine( // Change 20260726
        this.page(), // Change 20260726
        PAGE_SIZE, // Change 20260726
        this.status() || undefined, // Change 20260726
        this.sort(), // Change 20260726
      ); // Change 20260726
      this.items.set(result.items ?? []); // Change 20260726
      this.total.set(result.total ?? 0); // Change 20260726
    } catch { // Change 20260726
      this.items.set([]); // Change 20260726
      this.error.set($localize`:@@ideas.mine.error:Could not load ideas — try again`); // Change 20260726
    } finally { // Change 20260726
      this.loading.set(false); // Change 20260726
    } // Change 20260726
  } // Change 20260726

  onStatusChange(value: string): void { // Change 20260726
    this.status.set(value); // Change 20260726
    this.page.set(1); // Change 20260726
    void this.reload(); // Change 20260726
  } // Change 20260726

  onSortChange(value: string): void { // Change 20260726
    this.sort.set(value); // Change 20260726
    this.page.set(1); // Change 20260726
    void this.reload(); // Change 20260726
  } // Change 20260726

  goToPage(page: number): void { // Change 20260726
    if (page < 1 || page > this.totalPages()) return; // Change 20260726
    this.page.set(page); // Change 20260726
    void this.reload(); // Change 20260726
  } // Change 20260726

  title(item: MineIdeaRow): string { // Change 20260726
    return item.titleEn?.trim() || item.titleAr?.trim() || item.code; // Change 20260726
  } // Change 20260726

  toneClass(status: string): string { // Change 20260726
    return STATUS_TONES[status?.toLowerCase()] ?? 'bg-slate-100 text-slate-700'; // Change 20260726
  } // Change 20260726

  isEditable(item: MineIdeaRow): boolean { // Change 20260726
    return EDITABLE_STATUSES.has(item.status?.toLowerCase()); // Change 20260726
  } // Change 20260726
} // Change 20260726
