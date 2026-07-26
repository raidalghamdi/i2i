export interface InvitationReminderSettings {
  enabled: boolean;
  cronExpression: string;
  timezone: string;
  stopAfterNReminders: number;
  gapHours: number;
  expiresDays: number;
  fromName: string;
  fromEmail: string;
  programNameAr: string;
  programNameEn: string;
  updatedAt: string;
}

export type InvitationReminderSettingsInput = Partial<Omit<InvitationReminderSettings, 'updatedAt'>>;
