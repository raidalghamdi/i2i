import { NgTemplateOutlet } from '@angular/common';
import { Component, OnDestroy, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { IdentityService } from '../../core/auth/identity.service';
import { RoleSwitcherComponent } from '../../core/auth/role-switcher/role-switcher.component';
import { LocaleService } from '../../core/locale.service';
import { NotificationStore } from '../../core/notification-store';
import { IconComponent } from '../../shared/icon/icon.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { HeaderSearchComponent } from '../header-search/header-search.component';
import { NotificationBellComponent } from '../notification-bell/notification-bell.component';

const SIDEBAR_COLLAPSED_STORAGE_KEY = 'i2i-sidebar-collapsed';

interface NavItem {
  label: string;
  href: string;
  icon:
    | 'dashboard'
    | 'lightbulb'
    | 'clipboard-check'
    | 'users'
    | 'target'
    | 'settings'
    | 'chart-bar'
    | 'document-text'
    | 'alert-triangle'
    | 'shield-check'
    | 'stack'
    | 'rocket'
    | 'search';
}

interface NavGroup {
  label: string;
  items: NavItem[];
}

const ANY_ASSIGNED_ROLE = ['submitter', 'evaluator', 'judge', 'supervisor', 'admin'];
const EVALUATOR_AND_ABOVE = ['evaluator', 'judge', 'supervisor', 'admin'];
const SUPERVISOR_OR_COMMITTEE = ['supervisor', 'judge', 'admin'];
const SUPERVISOR_OR_ADMIN = ['supervisor', 'admin'];

@Component({
  selector: 'app-app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NgTemplateOutlet, RoleSwitcherComponent, IconComponent, LoadingStateComponent, ErrorStateComponent, HeaderSearchComponent, NotificationBellComponent],
  templateUrl: './app-shell.component.html',
})
export class AppShellComponent implements OnDestroy {
  private readonly identityService = inject(IdentityService);
  private readonly router = inject(Router);
  private readonly localeService = inject(LocaleService);
  private readonly notificationStore = inject(NotificationStore);
  readonly identity = this.identityService.identity;
  readonly loadFailed = this.identityService.loadFailed;

  /** Identity resolves asynchronously after the shell is created, so start the
   * notification poller reactively (idempotent) once the user has any role,
   * rather than only checking once in ngOnInit. */
  private readonly startNotificationsWhenAuthenticated = effect(() => {
    if ((this.identity()?.roles.length ?? 0) > 0) {
      this.notificationStore.start();
    }
  });

  ngOnDestroy(): void {
    this.notificationStore.stop();
  }

  protected readonly identityLoadingLabel = $localize`:@@identityLoading:Loading…`;
  protected readonly identityUnavailableMessage = $localize`:@@identityUnavailable:Unable to load your identity. Please refresh and try again.`;

  retryLoad(): void {
    void this.identityService.load();
  }

  /** The homepage-only section-jump nav should only show while on the homepage route. */
  readonly isHomeRoute = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects.split('?')[0].split('#')[0] === '/'),
    ),
    { initialValue: window.location.pathname === '/' },
  );

  readonly mobileNavOpen = signal(false);
  readonly sidebarCollapsed = signal(localStorage.getItem(SIDEBAR_COLLAPSED_STORAGE_KEY) === 'true');
  private readonly persistSidebarCollapsed = effect(() => {
    localStorage.setItem(SIDEBAR_COLLAPSED_STORAGE_KEY, String(this.sidebarCollapsed()));
  });

  private readonly activeRole = computed(() => this.identity()?.activeRole ?? null);
  private hasAnyRole(allowed: string[]): boolean {
    const active = this.activeRole();
    return active !== null && allowed.includes(active);
  }

  readonly navGroups = computed<NavGroup[]>(() => {
    const groups: NavGroup[] = [];

    if (this.hasAnyRole(ANY_ASSIGNED_ROLE)) {
      groups.push({
        label: $localize`:@@navGroupIdeas:Ideas`,
        items: [
          { label: $localize`:@@navMyIdeas:My ideas`, href: '/my-ideas', icon: 'lightbulb' },
          { label: $localize`:@@navExploreIdeas:Explore ideas`, href: '/ideas', icon: 'search' },
        ],
      });
    }
    if (this.hasAnyRole(EVALUATOR_AND_ABOVE)) {
      groups.push({
        label: $localize`:@@navGroupEvaluation:Evaluation`,
        items: [
          { label: $localize`:@@navEvaluatorDashboard:Dashboard`, href: '/evaluator', icon: 'dashboard' },
          { label: $localize`:@@navEvaluationQueue:Evaluation queue`, href: '/evaluations/queue', icon: 'clipboard-check' },
          { label: $localize`:@@navMyEvaluations:My evaluations`, href: '/evaluations/mine', icon: 'clipboard-check' },
          { label: $localize`:@@navApprovals:Approvals`, href: '/approvals', icon: 'clipboard-check' },
        ],
      });
    }
    if (this.hasAnyRole(SUPERVISOR_OR_COMMITTEE)) {
      groups.push({
        label: $localize`:@@navGroupCommittee:Committee`,
        items: [
          { label: $localize`:@@navCommitteeQueue:Committee queue`, href: '/committee/queue', icon: 'users' },
          { label: $localize`:@@navMyDecisions:My decisions`, href: '/committee/mine', icon: 'users' },
          { label: $localize`:@@navAnalytics:Analytics`, href: '/analytics', icon: 'chart-bar' },
        ],
      });
    }
    if (this.hasAnyRole(SUPERVISOR_OR_ADMIN)) {
      groups.push({
        label: $localize`:@@navGroupSupervision:Supervision`,
        items: [
          { label: $localize`:@@navSupervisorDashboard:Dashboard`, href: '/supervisor', icon: 'target' },
          { label: $localize`:@@navScreeningQueue:Screening queue`, href: '/supervisor/screening', icon: 'clipboard-check' },
          { label: $localize`:@@navTrackAssignments:Evaluator assignments`, href: '/supervisor/track-assignments', icon: 'users' },
          { label: $localize`:@@navSupervisorPhases:Phase scheduling`, href: '/admin/phases', icon: 'settings' },
          { label: $localize`:@@navSupervisorInvitationTemplates:Invitation templates`, href: '/admin/invitation-templates', icon: 'document-text' },
          { label: $localize`:@@navSupervisorRoster:Roster`, href: '/admin/roster', icon: 'users' },
          { label: $localize`:@@navSupervisorEmployeeImport:Employee import`, href: '/admin/employees/import', icon: 'users' },
          { label: $localize`:@@navSupervisorAssignments:Assignments`, href: '/admin/assignments', icon: 'clipboard-check' },
          { label: $localize`:@@navSupervisorCms:Content`, href: '/supervisor/cms', icon: 'stack' },
          { label: $localize`:@@navSupervisorEscalations:Escalations`, href: '/supervisor/escalations', icon: 'alert-triangle' },
          { label: $localize`:@@navSupervisorAnalytics:Analytics`, href: '/supervisor/analytics', icon: 'chart-bar' },
          { label: $localize`:@@navSupervisorReports:Reports`, href: '/supervisor/reports', icon: 'document-text' },
          { label: $localize`:@@navSupervisorAllIdeas:All ideas`, href: '/admin/all-ideas', icon: 'lightbulb' },
          { label: $localize`:@@navSupervisorFinalRanking:Final ranking`, href: '/admin/final-ranking', icon: 'target' },
        ],
      });
    }
    if (this.hasAnyRole(['admin'])) {
      groups.push({
        label: $localize`:@@navGroupAdministration:Administration`,
        items: [
          { label: $localize`:@@navAdminDashboard:Dashboard`, href: '/admin', icon: 'dashboard' },
          { label: $localize`:@@navAdminUsers:Users & roles`, href: '/admin/users', icon: 'shield-check' },
          { label: $localize`:@@navAdminCommitteeCriteria:Committee criteria`, href: '/admin/committee-criteria', icon: 'clipboard-check' },
          { label: $localize`:@@navAdminSettings:Platform settings`, href: '/admin/settings', icon: 'settings' },
          { label: $localize`:@@navAdminRoles:Roles`, href: '/admin/roles', icon: 'users' },
          { label: $localize`:@@navAdminTerms:Terms`, href: '/admin/cms/content', icon: 'document-text' },
          { label: $localize`:@@navAdminEvaluatorAssignments:Evaluator assignments`, href: '/admin/evaluator-assignments', icon: 'users' },
          { label: $localize`:@@navAdminPhases:Phase scheduling`, href: '/admin/phases', icon: 'settings' },
          { label: $localize`:@@navAdminInvitationTemplates:Invitation templates`, href: '/admin/invitation-templates', icon: 'document-text' },
          { label: $localize`:@@navAdminRoster:Roster`, href: '/admin/roster', icon: 'users' },
          { label: $localize`:@@navAdminEmployeeImport:Employee import`, href: '/admin/employees/import', icon: 'users' },
          { label: $localize`:@@navAdminAssignments:Assignments`, href: '/admin/assignments', icon: 'clipboard-check' },
          { label: $localize`:@@navAdminCms:Content`, href: '/admin/cms', icon: 'stack' },
          { label: $localize`:@@navAdminChallenges:Challenges`, href: '/admin/challenges', icon: 'target' },
          { label: $localize`:@@navAdminEscalations:Escalations`, href: '/admin/escalations', icon: 'alert-triangle' },
          { label: $localize`:@@navAdminPostProgram:Post-program`, href: '/admin/post-program', icon: 'rocket' },
          { label: $localize`:@@navAdminAnalytics:Analytics`, href: '/admin/analytics', icon: 'chart-bar' },
          { label: $localize`:@@navAdminReports:Reports`, href: '/admin/reports', icon: 'document-text' },
          { label: $localize`:@@navAdminReportTitles:Report titles`, href: '/admin/report-titles', icon: 'document-text' },
          { label: $localize`:@@navAdminInvitationSettings:Invitation settings`, href: '/admin/invitation-settings', icon: 'settings' },
          { label: $localize`:@@navAdminEvaluationSettings:Evaluation settings`, href: '/admin/evaluation-settings', icon: 'settings' },
          { label: $localize`:@@navAdminAudit:Audit log`, href: '/admin/audit', icon: 'shield-check' },
          { label: $localize`:@@navAdminEmailLog:Email log`, href: '/admin/email-log', icon: 'document-text' },
          { label: $localize`:@@navAdminSupport:Support`, href: '/admin/support', icon: 'alert-triangle' },
          { label: $localize`:@@navAdminCompliance:Compliance`, href: '/admin/compliance', icon: 'shield-check' },
          { label: $localize`:@@navAdminAllIdeas:All ideas`, href: '/admin/all-ideas', icon: 'lightbulb' },
          { label: $localize`:@@navAdminFinalRanking:Final ranking`, href: '/admin/final-ranking', icon: 'target' },
          { label: $localize`:@@navAdminBackup:Backup`, href: '/admin/backup', icon: 'stack' },
        ],
      });
    }

    return groups;
  });

  readonly canSubmitIdea = computed(() => this.hasAnyRole(ANY_ASSIGNED_ROLE));

  toggleMobileNav(): void {
    this.mobileNavOpen.update((v) => !v);
  }

  closeMobileNav(): void {
    this.mobileNavOpen.set(false);
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update((v) => !v);
  }

  alternateLocaleHref(): string {
    return this.localeService.alternateLocaleHref();
  }
}
