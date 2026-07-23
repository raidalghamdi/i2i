using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Data;

public class InnovationDbContext : DbContext
{
    public InnovationDbContext(DbContextOptions<InnovationDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PendingRoleGrant> PendingRoleGrants => Set<PendingRoleGrant>();
    public DbSet<IdeaStatus> IdeaStatuses => Set<IdeaStatus>();
    public DbSet<StrategicTheme> StrategicThemes => Set<StrategicTheme>();
    public DbSet<PhaseSchedule> PhaseSchedules => Set<PhaseSchedule>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Idea> Ideas => Set<Idea>();
    public DbSet<AssignmentStatus> AssignmentStatuses => Set<AssignmentStatus>();
    public DbSet<CommitteeDecisionType> CommitteeDecisionTypes => Set<CommitteeDecisionType>();
    public DbSet<Evaluation> Evaluations => Set<Evaluation>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<EvaluatorTrackAssignment> EvaluatorTrackAssignments => Set<EvaluatorTrackAssignment>();
    public DbSet<CommitteeCriterion> CommitteeCriteria => Set<CommitteeCriterion>();
    public DbSet<CommitteeDecision> CommitteeDecisions => Set<CommitteeDecision>();
    public DbSet<AdminSetting> AdminSettings => Set<AdminSetting>();
    public DbSet<RoleInvitation> RoleInvitations => Set<RoleInvitation>();
    public DbSet<RoleInvitationStatus> RoleInvitationStatuses => Set<RoleInvitationStatus>();
    public DbSet<RoleInvitationSettings> RoleInvitationSettings => Set<RoleInvitationSettings>();
    public DbSet<PilotStatus> PilotStatuses => Set<PilotStatus>();
    public DbSet<BenefitType> BenefitTypes => Set<BenefitType>();
    public DbSet<BenefitCategory> BenefitCategories => Set<BenefitCategory>();
    public DbSet<FundingStatus> FundingStatuses => Set<FundingStatus>();
    public DbSet<Pilot> Pilots => Set<Pilot>();
    public DbSet<Benefit> Benefits => Set<Benefit>();
    public DbSet<FundingRequest> FundingRequests => Set<FundingRequest>();
    public DbSet<ScaleDecisionType> ScaleDecisionTypes => Set<ScaleDecisionType>();
    public DbSet<HandoverStatus> HandoverStatuses => Set<HandoverStatus>();
    public DbSet<ScaleDecision> ScaleDecisions => Set<ScaleDecision>();
    public DbSet<Implementation> Implementations => Set<Implementation>();
    public DbSet<IpType> IpTypes => Set<IpType>();
    public DbSet<KnowledgeType> KnowledgeTypes => Set<KnowledgeType>();
    public DbSet<KnowledgeVisibility> KnowledgeVisibilities => Set<KnowledgeVisibility>();
    public DbSet<IpRecord> IpRecords => Set<IpRecord>();
    public DbSet<IpSignature> IpSignatures => Set<IpSignature>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
    public DbSet<StandardBody> StandardBodies => Set<StandardBody>();
    public DbSet<ComplianceControlStatus> ComplianceControlStatuses => Set<ComplianceControlStatus>();
    public DbSet<ComplianceControl> ComplianceControls => Set<ComplianceControl>();
    public DbSet<TeamMemberRole> TeamMemberRoles => Set<TeamMemberRole>();
    public DbSet<TeamInvitationStatus> TeamInvitationStatuses => Set<TeamInvitationStatus>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamInvitation> TeamInvitations => Set<TeamInvitation>();
    public DbSet<EmailOutboxStatus> EmailOutboxStatuses => Set<EmailOutboxStatus>();
    public DbSet<EmailLogStatus> EmailLogStatuses => Set<EmailLogStatus>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<InvitationReminderSettings> InvitationReminderSettings => Set<InvitationReminderSettings>();
    public DbSet<EmailOutbox> EmailOutboxes => Set<EmailOutbox>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailTemplateAttachment> EmailTemplateAttachments => Set<EmailTemplateAttachment>();
    public DbSet<EscalationTier> EscalationTiers => Set<EscalationTier>();
    public DbSet<EscalationStatus> EscalationStatuses => Set<EscalationStatus>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<SlaTracking> SlaTrackings => Set<SlaTracking>();
    public DbSet<Escalation> Escalations => Set<Escalation>();
    public DbSet<EscalationEvent> EscalationEvents => Set<EscalationEvent>();
    public DbSet<ApprovalInstanceStatus> ApprovalInstanceStatuses => Set<ApprovalInstanceStatus>();
    public DbSet<ApprovalDecisionType> ApprovalDecisionTypes => Set<ApprovalDecisionType>();
    public DbSet<ApprovalChain> ApprovalChains => Set<ApprovalChain>();
    public DbSet<ApprovalChainStep> ApprovalChainSteps => Set<ApprovalChainStep>();
    public DbSet<ApprovalInstance> ApprovalInstances => Set<ApprovalInstance>();
    public DbSet<ApprovalStepDecision> ApprovalStepDecisions => Set<ApprovalStepDecision>();
    public DbSet<EvidenceAttachment> EvidenceAttachments => Set<EvidenceAttachment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<CmsBlock> CmsBlocks => Set<CmsBlock>();
    public DbSet<ReportTitle> ReportTitles => Set<ReportTitle>();
    public DbSet<CmsContent> CmsContents => Set<CmsContent>();
    public DbSet<ContentString> ContentStrings => Set<ContentString>();
    public DbSet<ReportGeneration> ReportGenerations => Set<ReportGeneration>();
    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<IdeaTeamMember> IdeaTeamMembers => Set<IdeaTeamMember>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InnovationDbContext).Assembly);
    }
}
