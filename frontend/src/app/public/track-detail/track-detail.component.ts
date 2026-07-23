import { Component, Inject, LOCALE_ID, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/icon/icon.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { PublicTracksApiService } from '../../core/public-tracks-api.service';
import { PublicChallenge, PublicIdeaSummary, PublicTrackDetail } from '../../core/public-data.model';

@Component({
  selector: 'app-track-detail',
  imports: [RouterLink, IconComponent, StatusBadgeComponent, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './track-detail.component.html',
})
export class TrackDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(PublicTracksApiService);
  private readonly isArabic: boolean;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly detail = signal<PublicTrackDetail | null>(null);

  readonly backToTracksLabel = $localize`:@@trackDetailBackToTracks:Back to Tracks`;
  readonly notFoundMessage = $localize`:@@trackDetailNotFound:Track not found.`;
  readonly submitIdeaLabel = $localize`:@@trackDetailSubmitIdea:Submit an idea`;
  readonly challengesTitle = $localize`:@@trackDetailChallengesTitle:Challenges`;
  readonly relatedIdeasTitle = $localize`:@@trackDetailRelatedIdeasTitle:Related ideas`;
  readonly noRelatedIdeasMessage = $localize`:@@trackDetailNoRelatedIdeas:No related ideas yet.`;
  readonly strategyTitle = $localize`:@@trackDetailStrategyTitle:Strategy`;
  readonly strategyBody = $localize`:@@trackDetailStrategyBody:This track focuses submissions on a specific strategic priority, helping evaluators assess alignment and impact within a shared scope.`;
  readonly benefitsTitle = $localize`:@@trackDetailBenefitsTitle:Benefits`;
  readonly benefitsBody = $localize`:@@trackDetailBenefitsBody:Grouping ideas by track lets specialized evaluators focus their expertise, and lets successful pilots scale within a coherent theme.`;

  readonly defaultChallenges: readonly string[] = [
    $localize`:@@trackDetailDefaultChallenge1:Identify a real challenge within the track's scope`,
    $localize`:@@trackDetailDefaultChallenge2:Propose a practical, applicable solution`,
    $localize`:@@trackDetailDefaultChallenge3:Measure the expected impact on competition`,
  ];

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.loading.set(true);
    this.error.set(null);
    try {
      this.detail.set(await this.api.getById(id));
    } catch (err) {
      this.error.set(this.extractErrorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  trackName(detail: PublicTrackDetail): string {
    return this.isArabic ? detail.track.nameAr : detail.track.nameEn;
  }

  trackDescription(detail: PublicTrackDetail): string {
    return this.isArabic ? detail.track.descriptionAr : detail.track.descriptionEn;
  }

  challengeText(challenge: PublicChallenge): string {
    return this.isArabic ? challenge.textAr : challenge.textEn;
  }

  ideaTitle(idea: PublicIdeaSummary): string {
    return this.isArabic ? idea.titleAr : idea.titleEn;
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@trackDetailLoadError:Couldn't load this track. Please try again.`;
  }
}
