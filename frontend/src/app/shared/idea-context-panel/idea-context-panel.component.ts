import { Component, computed, input } from '@angular/core';
import { Idea } from '../../ideas/idea.model';
import { EvaluationReviewDetail } from '../../evaluations/evaluation-review.model';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';
import { StatusLabelPipe } from '../status-label/status-label.pipe';

export type IdeaContextViewMode = 'evaluator' | 'submitter' | 'supervisor' | 'judge';

/**
 * Shared read-only summary of an idea: content, classification, attachments, and (depending on
 * `viewMode`) team identity and evaluator ratings. Embedded on every stage-action page so the
 * decision-maker always has the idea in view alongside their decision form.
 *
 * Identity/rating visibility rules (see FE-1 brief):
 * - Team identity (`teamName`/`teamMembers`) renders for `supervisor`, `judge`, and `submitter`
 *   — never for `evaluator` (evaluations must stay unbiased; the backend already nulls identity
 *   fields for evaluator-scoped idea reads, but this component enforces it defensively too).
 * - Evaluator ratings (`evaluationDetail`) render only for `supervisor` and `judge`, and only
 *   when the caller actually provides the detail.
 */
@Component({
  selector: 'app-idea-context-panel',
  imports: [StatusBadgeComponent, StatusLabelPipe],
  templateUrl: './idea-context-panel.component.html',
})
export class IdeaContextPanelComponent {
  readonly idea = input.required<Idea>();
  readonly viewMode = input.required<IdeaContextViewMode>();
  readonly themeName = input<string | null>(null);
  readonly activityName = input<string | null>(null);
  readonly evaluationDetail = input<EvaluationReviewDetail | null>(null);

  readonly showTeam = computed(() => this.viewMode() !== 'evaluator');

  readonly showEvaluationDetail = computed(() => {
    const mode = this.viewMode();
    return (mode === 'supervisor' || mode === 'judge') && this.evaluationDetail() !== null;
  });
}
