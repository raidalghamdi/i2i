import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { IdeasApiService } from '../ideas/ideas-api.service';
import { Idea } from '../ideas/idea.model';
import { LoadingStateComponent } from '../shared/loading-state/loading-state.component';

/** Post-submit confirmation page. On a fetch failure, deliberately redirects
 * to the idea detail page instead of showing an error card (e.g. a stale or
 * unauthorized id) — so there is a `loading` gate for the brief fetch, but
 * intentionally no `app-error-state`. */
@Component({
  selector: 'app-idea-submitted',
  imports: [RouterLink, LoadingStateComponent],
  templateUrl: './idea-submitted.component.html',
})
export class IdeaSubmittedComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly ideasApi = inject(IdeasApiService);

  readonly idea = signal<Idea | null>(null);
  readonly loading = signal(true);

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loading.set(true);
    try {
      this.idea.set(await this.ideasApi.getById(id));
    } catch {
      await this.router.navigate(['/ideas', id]);
    } finally {
      this.loading.set(false);
    }
  }
}
