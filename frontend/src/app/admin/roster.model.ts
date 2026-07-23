export interface RosterHubRow {
  roleCode: string;
  roleNameAr: string;
  roleNameEn: string;
  activeCount: number;
  pendingCount: number;
  expiredCount: number;
  withdrawnCount: number;
}

export interface RosterActiveMember {
  userId: string;
  samAccountName: string;
  fullNameAr: string;
  fullNameEn: string;
  email: string;
  isActive: boolean;
}

export type RoleInvitationStatus = 'pending' | 'applied' | 'expired' | 'withdrawn';

export interface RoleInvitation {
  id: string;
  samAccountName: string;
  displayName: string | null;
  email: string | null;
  status: RoleInvitationStatus;
  deadlineAt: string | null;
  respondedAt: string | null;
  reminderCount: number;
  lastReminderAt: string | null;
  source: 'manual' | 'import';
  invitedByName: string;
  createdAt: string;
}

export interface RosterRoleDetail {
  roleCode: string;
  roleNameAr: string;
  roleNameEn: string;
  activeMembers: RosterActiveMember[];
  invitations: RoleInvitation[];
}

export interface BulkCreateResult {
  total: number;
  created: number;
  skipped: number;
  errors: { samAccountName: string; message: string }[];
}

export interface RoleInvitationSettings {
  enabled: boolean;
  defaultExpiresDays: number;
  reminderGapHours: number;
  maxReminders: number;
  updatedAt: string;
}

export interface RoleInvitationSettingsInput {
  enabled?: boolean;
  defaultExpiresDays?: number;
  reminderGapHours?: number;
  maxReminders?: number;
}
