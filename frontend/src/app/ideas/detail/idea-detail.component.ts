import { DatePipe } from '@angular/common'; // Change 20260726
import { HttpErrorResponse } from '@angular/common/http'; // Change 20260726
import { Component, OnInit, computed, inject, signal } from '@angular/core'; // Change 20260726
import { ActivatedRoute, RouterLink } from '@angular/router'; // Change 20260726
import { IdeaAuditEntry, IdeaDetailView, IdeasService } from '../ideas.service'; // Change 20260726
import { StatusLabelPipe } from '../../shared/status-label/status-label.pipe'; // Change 20260726
import { StrategicThemesService } from '../strategic-themes.service'; // Change 20260726

/** Statuses that hand the idea back to its author, so the feedback banner applies. */ // Change 20260726
const FEEDBACK_STATUSES = new Set(['returned', 'needs_completion']); // Change 20260726

/** One rendered timeline row, derived from an audit log entry. */ // Change 20260726
export interface AuditTimelineRow { // Change 20260726
  action: string; // Change 20260726
  fromStatus: string | null; // Change 20260726
  toStatus: string | null; // Change 20260726
  changedBy: string | null; // Change 20260726
  changedAt: string; // Change 20260726
  notes: string | null; // Change 20260726
} // Change 20260726

@Component({ // Change 20260726
  selector: 'app-idea-detail-view', // Change 20260726
  imports: [DatePipe, RouterLink, StatusLabelPipe], // Change 20260726
  templateUrl: './idea-detail.component.html', // Change 20260726
  styleUrl: './idea-detail.component.scss', // Change 20260726
}) // Change 20260726
export class IdeaDetailViewComponent implements OnInit { // Change 20260726
  private readonly ideas = inject(IdeasService); // Change 20260726
  private readonly themes = inject(StrategicThemesService); // Change 20260726
  private readonly route = inject(ActivatedRoute); // Change 20260726

  readonly idea = signal<IdeaDetailView | null>(null); // Change 20260726
  readonly themeName = signal<string | null>(null); // Change 20260726
  readonly loading = signal(false); // Change 20260726
  readonly notFound = signal(false); // Change 20260726
  readonly error = signal<string | null>(null); // Change 20260726

  readonly ideaId = computed(() => this.idea()?.id ?? ''); // Change 20260726
  readonly showFeedback = computed(() => FEEDBACK_STATUSES.has(this.idea()?.status ?? '')); // Change 20260726

  readonly timeline = computed<AuditTimelineRow[]>(() => // Change 20260726
    (this.idea()?.auditTrail ?? []).map((entry) => this.toTimelineRow(entry)), // Change 20260726
  ); // Change 20260726

  ngOnInit(): void { // Change 20260726
    void this.reload(); // Change 20260726
  } // Change 20260726

  async reload(): Promise<void> { // Change 20260726
    const id = this.route.snapshot.paramMap.get('id') ?? ''; // Change 20260726
    this.loading.set(true); // Change 20260726
    this.error.set(null); // Change 20260726
    this.notFound.set(false); // Change 20260726
    try { // Change 20260726
      const idea = await this.ideas.getById(id); // Change 20260726
      this.idea.set(idea); // Change 20260726
      void this.loadThemeName(idea.strategicThemeId); // Change 20260726
    } catch (err) { // Change 20260726
      this.idea.set(null); // Change 20260726
      if (err instanceof HttpErrorResponse && (err.status === 404 || err.status === 403)) { // Change 20260726
        this.notFound.set(true); // Change 20260726
      } else { // Change 20260726
        this.error.set($localize`:@@ideas.detail.error:Could not load this idea — try again`); // Change 20260726
      } // Change 20260726
    } finally { // Change 20260726
      this.loading.set(false); // Change 20260726
    } // Change 20260726
  } // Change 20260726

  /** The detail payload carries only the theme id, so the name is resolved from the catalogue. */ // Change 20260726
  private async loadThemeName(themeId: string | null): Promise<void> { // Change 20260726
    if (!themeId) return; // Change 20260726
    try { // Change 20260726
      const all = await this.themes.list(); // Change 20260726
      const match = all.find((t) => t.id === themeId); // Change 20260726
      this.themeName.set(match ? match.nameEn || match.nameAr : null); // Change 20260726
    } catch { // Change 20260726
      this.themeName.set(null); // Change 20260726
    } // Change 20260726
  } // Change 20260726

  /** Audit payloads are opaque JSON strings; pull out status transition fields when present. */ // Change 20260726
  private toTimelineRow(entry: IdeaAuditEntry): AuditTimelineRow { // Change 20260726
    let payload: Record<string, unknown> = {}; // Change 20260726
    if (entry.payload) { // Change 20260726
      try { // Change 20260726
        const parsed: unknown = JSON.parse(entry.payload); // Change 20260726
        if (parsed && typeof parsed === 'object') payload = parsed as Record<string, unknown>; // Change 20260726
      } catch { // Change 20260726
        payload = {}; // Change 20260726
      } // Change 20260726
    } // Change 20260726
    const text = (key: string): string | null => { // Change 20260726
      const value = payload[key]; // Change 20260726
      return typeof value === 'string' && value.trim() ? value : null; // Change 20260726
    }; // Change 20260726
    return { // Change 20260726
      action: entry.action, // Change 20260726
      fromStatus: text('fromStatus') ?? text('from'), // Change 20260726
      toStatus: text('toStatus') ?? text('to') ?? text('status'), // Change 20260726
      changedBy: text('actorName') ?? entry.actorId, // Change 20260726
      changedAt: entry.occurredAt, // Change 20260726
      notes: text('notes') ?? text('reason') ?? text('comment'), // Change 20260726
    }; // Change 20260726
  } // Change 20260726

  feedbackText(): string | null { // Change 20260726
    return this.idea()?.screeningReason ?? null; // Change 20260726
  } // Change 20260726

  revisionCount(): number { // Change 20260726
    return (this.idea()?.auditTrail ?? []).filter((a) => a.action.includes('resubmit')).length; // Change 20260726
  } // Change 20260726

  fileSize(bytes: number): string { // Change 20260726
    if (bytes < 1024) return `${bytes} B`; // Change 20260726
    if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`; // Change 20260726
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`; // Change 20260726
  } // Change 20260726
} // Change 20260726
