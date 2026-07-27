import { Injectable, inject } from '@angular/core';
import { IdeasApiService } from '../ideas/ideas-api.service';

/**
 * Change 20260726
 *
 * Answers "did I submit this idea?" for the reviewer queues, which need it to hide the
 * evaluate/decide affordance on the reviewer's own ideas. The queue payloads carry no submitter id,
 * so membership is derived from the caller's own ideas instead. Returns an empty set if the lookup
 * fails: the backend rejects a self-authored submission regardless, so a failed hint must not
 * block reviewing everything else.
 */
@Injectable({ providedIn: 'root' })
export class OwnIdeasService {
  private readonly ideasApi = inject(IdeasApiService);

  async loadOwnIdeaIds(): Promise<Set<string>> {
    try {
      const mine = await this.ideasApi.getMine();
      return new Set(mine.map((idea) => idea.id));
    } catch {
      return new Set<string>();
    }
  }
}
