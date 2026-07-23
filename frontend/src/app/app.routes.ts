import { Routes } from '@angular/router';
import {
  adminOnlyGuard,
  anyAssignedRoleGuard,
  evaluatorAndAboveGuard,
  supervisorOrAdminGuard,
  supervisorOrCommitteeGuard,
} from './core/auth/guards/role-guards';
import { LandingComponent } from './landing/landing.component';
import { PublicShellComponent } from './shell/public-shell/public-shell.component';
import { AppShellComponent } from './shell/app-shell/app-shell.component';

export const routes: Routes = [
  {
    path: '',
    component: PublicShellComponent,
    children: [
      { path: '', component: LandingComponent },
      // Phase 3.1/3.2 public pages get added here as children.
      {
        path: 'privacy',
        loadComponent: () =>
          import('./public/privacy/privacy.component').then((m) => m.PrivacyComponent),
      },
      {
        path: 'terms',
        loadComponent: () =>
          import('./public/terms/terms.component').then((m) => m.TermsComponent),
      },
      {
        path: 'ip-terms',
        loadComponent: () =>
          import('./public/ip-terms/ip-terms.component').then((m) => m.IpTermsComponent),
      },
      {
        path: 'faq',
        loadComponent: () => import('./public/faq/faq.component').then((m) => m.FaqComponent),
      },
      {
        path: 'roadmap',
        loadComponent: () =>
          import('./public/roadmap/roadmap.component').then((m) => m.RoadmapComponent),
      },
      {
        path: 'about',
        loadComponent: () =>
          import('./public/about/about.component').then((m) => m.AboutComponent),
      },
      {
        path: 'target-audience',
        loadComponent: () =>
          import('./public/target-audience/target-audience.component').then(
            (m) => m.TargetAudienceComponent,
          ),
      },
      {
        path: 'expected-solutions',
        loadComponent: () =>
          import('./public/expected-solutions/expected-solutions.component').then(
            (m) => m.ExpectedSolutionsComponent,
          ),
      },
      {
        path: 'evaluation-criteria',
        loadComponent: () =>
          import('./public/evaluation-criteria/evaluation-criteria.component').then(
            (m) => m.EvaluationCriteriaComponent,
          ),
      },
      {
        path: 'partners',
        loadComponent: () =>
          import('./public/partners/partners.component').then((m) => m.PartnersComponent),
      },
      {
        path: 'events',
        loadComponent: () =>
          import('./public/events/events.component').then((m) => m.EventsComponent),
      },
      {
        path: 'events/:section',
        loadComponent: () =>
          import('./public/event-section/event-section.component').then(
            (m) => m.EventSectionComponent,
          ),
      },
      {
        path: 'stages',
        loadComponent: () =>
          import('./public/stages/stages.component').then((m) => m.StagesComponent),
      },
      {
        path: 'pilots',
        loadComponent: () =>
          import('./public/pilots/pilots.component').then((m) => m.PilotsComponent),
      },
      {
        path: 'support',
        loadComponent: () =>
          import('./public/support/support.component').then((m) => m.SupportComponent),
      },
      {
        path: 'tracks',
        loadComponent: () =>
          import('./public/tracks/tracks.component').then((m) => m.TracksComponent),
      },
      {
        path: 'tracks/:id',
        loadComponent: () =>
          import('./public/track-detail/track-detail.component').then(
            (m) => m.TrackDetailComponent,
          ),
      },
      {
        path: 'activities',
        loadComponent: () =>
          import('./public/activities/activities.component').then((m) => m.ActivitiesComponent),
      },
      {
        path: 'search',
        loadComponent: () =>
          import('./public/search/search.component').then((m) => m.SearchComponent),
      },
      {
        path: 'activities/:id',
        loadComponent: () =>
          import('./public/activity-detail/activity-detail.component').then(
            (m) => m.ActivityDetailComponent,
          ),
      },
    ],
  },
  {
    path: '',
    component: AppShellComponent,
    children: [
      {
        path: 'ideas',
        loadComponent: () =>
          import('./ideas/ideas-explorer/ideas-explorer.component').then(
            (m) => m.IdeasExplorerComponent,
          ),
        canActivate: [anyAssignedRoleGuard],
      },
      {
        path: 'ideas/new',
        loadComponent: () =>
          import('./ideas/idea-submit-wizard/idea-submit-wizard.component').then(
            (m) => m.IdeaSubmitWizardComponent,
          ),
        canActivate: [anyAssignedRoleGuard],
      },
      {
        path: 'my-ideas',
        loadComponent: () =>
          import('./ideas/my-ideas/my-ideas.component').then((m) => m.MyIdeasComponent),
        canActivate: [anyAssignedRoleGuard],
      },
      {
        path: 'ideas/:id/edit',
        loadComponent: () =>
          import('./ideas/idea-form/idea-form.component').then((m) => m.IdeaFormComponent),
        canActivate: [anyAssignedRoleGuard],
      },
      {
        path: 'app-search',
        loadComponent: () =>
          import('./search-results/search-results.component').then((m) => m.SearchResultsComponent),
        canActivate: [anyAssignedRoleGuard],
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./dashboard/dashboard-router/dashboard-router.component').then(
            (m) => m.DashboardRouterComponent,
          ),
        canActivate: [anyAssignedRoleGuard],
      },
      {
        path: 'ideas/:id',
        loadComponent: () =>
          import('./ideas/idea-detail/idea-detail.component').then((m) => m.IdeaDetailComponent),
        canActivate: [anyAssignedRoleGuard],
      },
      { path: 'profile', redirectTo: 'settings' },
      {
        path: 'profile/level',
        loadComponent: () =>
          import('./profile-level/profile-level.component').then((m) => m.ProfileLevelComponent),
        canActivate: [anyAssignedRoleGuard],
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./settings/settings.component').then((m) => m.SettingsComponent),
        canActivate: [anyAssignedRoleGuard],
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./notifications/notifications.component').then((m) => m.NotificationsComponent),
        canActivate: [anyAssignedRoleGuard],
      },
      {
        path: 'ideas/:id/submitted',
        loadComponent: () =>
          import('./idea-submitted/idea-submitted.component').then((m) => m.IdeaSubmittedComponent),
        canActivate: [anyAssignedRoleGuard],
      },
      {
        path: 'evaluations/queue',
        loadComponent: () =>
          import('./evaluations/evaluator-queue/evaluator-queue.component').then(
            (m) => m.EvaluatorQueueComponent,
          ),
        canActivate: [evaluatorAndAboveGuard],
      },
      {
        path: 'evaluations/mine',
        loadComponent: () =>
          import('./evaluations/my-evaluations-list/my-evaluations-list.component').then(
            (m) => m.MyEvaluationsListComponent,
          ),
        canActivate: [evaluatorAndAboveGuard],
      },
      {
        path: 'evaluations/:id',
        loadComponent: () =>
          import('./evaluations/evaluation-form/evaluation-form.component').then(
            (m) => m.EvaluationFormComponent,
          ),
        canActivate: [evaluatorAndAboveGuard],
      },
      {
        path: 'approvals',
        loadComponent: () =>
          import('./approvals/approval-queue/approval-queue.component').then(
            (m) => m.ApprovalQueueComponent,
          ),
        canActivate: [evaluatorAndAboveGuard],
      },
      {
        path: 'evaluator/level',
        loadComponent: () =>
          import('./evaluator-level/evaluator-level.component').then(
            (m) => m.EvaluatorLevelComponent,
          ),
        canActivate: [evaluatorAndAboveGuard],
      },
      {
        path: 'evaluator',
        loadComponent: () =>
          import('./evaluator-dashboard/evaluator-dashboard.component').then(
            (m) => m.EvaluatorDashboardComponent,
          ),
        canActivate: [evaluatorAndAboveGuard],
      },
      { path: 'evaluation', redirectTo: 'evaluator' },
      { path: 'evaluator/notifications', redirectTo: 'notifications' },
      { path: 'evaluator/settings', redirectTo: 'settings' },
      { path: 'supervisor/analytics', redirectTo: 'admin/analytics' },
      { path: 'supervisor/reports', redirectTo: 'admin/reports' },
      { path: 'supervisor/escalations', redirectTo: 'admin/escalations' },
      { path: 'supervisor/cms', redirectTo: 'admin/cms' },
      { path: 'supervisor/phases', redirectTo: 'admin/phases' },
      { path: 'supervisor/invitation-templates', redirectTo: 'admin/invitation-templates' },
      { path: 'supervisor/assignments', redirectTo: 'admin/assignments' },
      { path: 'admin/evaluator-assignments', redirectTo: 'supervisor/track-assignments' },
      {
        path: 'analytics',
        loadComponent: () =>
          import('./analytics/analytics-overview/analytics-overview.component').then(
            (m) => m.AnalyticsOverviewComponent,
          ),
        canActivate: [supervisorOrCommitteeGuard],
      },
      {
        path: 'analytics/pillars/:themeId',
        loadComponent: () =>
          import('./analytics/pillar-detail/pillar-detail.component').then(
            (m) => m.PillarDetailComponent,
          ),
        canActivate: [supervisorOrCommitteeGuard],
      },
      {
        path: 'committee/queue',
        loadComponent: () =>
          import('./committee/committee-queue/committee-queue.component').then(
            (m) => m.CommitteeQueueComponent,
          ),
        canActivate: [supervisorOrCommitteeGuard],
      },
      {
        path: 'committee/mine',
        loadComponent: () =>
          import('./committee/my-decisions-list/my-decisions-list.component').then(
            (m) => m.MyDecisionsListComponent,
          ),
        canActivate: [supervisorOrCommitteeGuard],
      },
      {
        path: 'committee/:id',
        loadComponent: () =>
          import('./committee/committee-decision-form/committee-decision-form.component').then(
            (m) => m.CommitteeDecisionFormComponent,
          ),
        canActivate: [supervisorOrCommitteeGuard],
      },
      {
        path: 'supervisor',
        loadComponent: () =>
          import('./supervisor/supervisor-dashboard/supervisor-dashboard.component').then(
            (m) => m.SupervisorDashboardComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'supervisor/screening',
        loadComponent: () =>
          import('./supervisor/screening-queue/screening-queue.component').then(
            (m) => m.ScreeningQueueComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'supervisor/screening/:id',
        loadComponent: () =>
          import('./supervisor/screening-decision-form/screening-decision-form.component').then(
            (m) => m.ScreeningDecisionFormComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'supervisor/track-assignments',
        loadComponent: () =>
          import('./supervisor/track-assignments/track-assignments.component').then(
            (m) => m.TrackAssignmentsComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin',
        loadComponent: () =>
          import('./admin/admin-dashboard/admin-dashboard.component').then(
            (m) => m.AdminDashboardComponent,
          ),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/analytics',
        loadComponent: () =>
          import('./admin/analytics-dashboard/analytics-dashboard.component').then(
            (m) => m.AnalyticsDashboardComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/reports',
        loadComponent: () =>
          import('./admin/reports-dashboard/reports-dashboard.component').then(
            (m) => m.ReportsDashboardComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/report-titles',
        loadComponent: () =>
          import('./admin/report-titles/report-titles.component').then(
            (m) => m.ReportTitlesComponent,
          ),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/users',
        loadComponent: () =>
          import('./admin/user-list/user-list.component').then((m) => m.UserListComponent),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/users/:id',
        loadComponent: () =>
          import('./admin/user-detail/user-detail.component').then((m) => m.UserDetailComponent),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/group-grant',
        loadComponent: () =>
          import('./admin/group-grant/group-grant.component').then((m) => m.GroupGrantComponent),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/cms',
        loadComponent: () =>
          import('./admin/cms-dashboard/cms-dashboard.component').then(
            (m) => m.CmsDashboardComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/cms/blocks',
        loadComponent: () =>
          import('./admin/cms-block-list/cms-block-list.component').then(
            (m) => m.CmsBlockListComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/cms/blocks/new',
        loadComponent: () =>
          import('./admin/cms-block-form/cms-block-form.component').then(
            (m) => m.CmsBlockFormComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/cms/blocks/:id/edit',
        loadComponent: () =>
          import('./admin/cms-block-form/cms-block-form.component').then(
            (m) => m.CmsBlockFormComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/cms/content',
        loadComponent: () =>
          import('./admin/cms-content-list/cms-content-list.component').then(
            (m) => m.CmsContentListComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/cms/content/new',
        loadComponent: () =>
          import('./admin/cms-content-form/cms-content-form.component').then(
            (m) => m.CmsContentFormComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/cms/content/:id/edit',
        loadComponent: () =>
          import('./admin/cms-content-form/cms-content-form.component').then(
            (m) => m.CmsContentFormComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/cms/strings',
        loadComponent: () =>
          import('./admin/content-string-list/content-string-list.component').then(
            (m) => m.ContentStringListComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/cms/strings/new',
        loadComponent: () =>
          import('./admin/content-string-form/content-string-form.component').then(
            (m) => m.ContentStringFormComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/cms/strings/:id/edit',
        loadComponent: () =>
          import('./admin/content-string-form/content-string-form.component').then(
            (m) => m.ContentStringFormComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/challenges',
        loadComponent: () =>
          import('./admin/challenge-list/challenge-list.component').then(
            (m) => m.ChallengeListComponent,
          ),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/challenges/new',
        loadComponent: () =>
          import('./admin/challenge-form/challenge-form.component').then(
            (m) => m.ChallengeFormComponent,
          ),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/challenges/:id/edit',
        loadComponent: () =>
          import('./admin/challenge-form/challenge-form.component').then(
            (m) => m.ChallengeFormComponent,
          ),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/post-program',
        loadComponent: () =>
          import('./admin/post-program-dashboard/post-program-dashboard.component').then(
            (m) => m.PostProgramDashboardComponent,
          ),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/compliance',
        loadComponent: () =>
          import('./admin/compliance/compliance.component').then((m) => m.ComplianceComponent),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/committee-criteria',
        loadComponent: () =>
          import('./admin/committee-criteria/committee-criteria.component').then(
            (m) => m.CommitteeCriteriaComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/roles',
        loadComponent: () =>
          import('./admin/roles-catalog/roles-catalog.component').then((m) => m.RolesCatalogComponent),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/escalations',
        loadComponent: () =>
          import('./admin/escalation-board/escalation-board.component').then(
            (m) => m.EscalationBoardComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/audit',
        loadComponent: () =>
          import('./admin/audit-browse/audit-browse.component').then(
            (m) => m.AuditBrowseComponent,
          ),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/email-log',
        loadComponent: () =>
          import('./admin/email-log/email-log.component').then((m) => m.EmailLogComponent),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/support',
        loadComponent: () =>
          import('./admin/support-inbox/support-inbox.component').then(
            (m) => m.SupportInboxComponent,
          ),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/phases',
        loadComponent: () =>
          import('./admin/admin-phases/admin-phases.component').then((m) => m.AdminPhasesComponent),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/invitation-templates',
        loadComponent: () =>
          import('./admin/email-template-editor/email-template-editor.component').then(
            (m) => m.EmailTemplateEditorComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/assignments',
        loadComponent: () =>
          import('./admin/assignments/assignments-page.component').then(
            (m) => m.AssignmentsPageComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/invitation-settings',
        loadComponent: () =>
          import('./admin/invitation-settings-form/invitation-settings-form.component').then(
            (m) => m.InvitationSettingsFormComponent,
          ),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/evaluation-settings',
        loadComponent: () =>
          import('./admin/evaluation-settings-form/evaluation-settings-form.component').then(
            (m) => m.EvaluationSettingsFormComponent,
          ),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/settings',
        loadComponent: () =>
          import('./admin/platform-settings/platform-settings.component').then(
            (m) => m.PlatformSettingsComponent,
          ),
        canActivate: [adminOnlyGuard],
      },
      {
        path: 'admin/roster',
        loadComponent: () =>
          import('./admin/roster-hub/roster-hub.component').then((m) => m.RosterHubComponent),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/roster/:roleCode',
        loadComponent: () =>
          import('./admin/roster-detail/roster-detail.component').then(
            (m) => m.RosterDetailComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/employees/import',
        loadComponent: () =>
          import('./admin/employee-import/employee-import.component').then(
            (m) => m.EmployeeImportComponent,
          ),
        canActivate: [supervisorOrAdminGuard],
      },
      { path: 'supervisor/roster', redirectTo: 'admin/roster' },
      { path: 'supervisor/employees/import', redirectTo: 'admin/employees/import' },
      {
        path: 'admin/all-ideas',
        loadComponent: () =>
          import('./admin/all-ideas/all-ideas-console.component').then((m) => m.AllIdeasConsoleComponent),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/final-ranking',
        loadComponent: () =>
          import('./admin/final-ranking/final-ranking-page.component').then((m) => m.FinalRankingPageComponent),
        canActivate: [supervisorOrAdminGuard],
      },
      {
        path: 'admin/backup',
        loadComponent: () => import('./admin/backup/backup.component').then((m) => m.BackupComponent),
        canActivate: [adminOnlyGuard],
      },
    ],
  },
];
