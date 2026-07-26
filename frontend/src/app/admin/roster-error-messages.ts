// Backend roster/employee-import endpoints (invite, bulk-invite, employee import) report per-row
// failures as `{ samAccountName, message }`, where `message` is the raw C# `.ToString()` of the
// `RoleInvitationCommandStatus` enum (see backend/src/InnovationToImpact.Domain/Roster/
// RoleInvitationCommandStatus.cs). The backend has no localization mechanism of its own, so this
// maps each known status code to a localized, human-readable string. Unknown codes fall back to
// the raw value unchanged so a future backend-added status never crashes or silently disappears.
export function translateRosterErrorCode(code: string): string {
  switch (code) {
    case 'AdUserNotFound':
      return $localize`:@@rosterErrorAdUserNotFound:AD account not found`;
    case 'AlreadyApplied':
      return $localize`:@@rosterErrorAlreadyApplied:This person already has this role`;
    case 'AlreadyPending':
      return $localize`:@@rosterErrorAlreadyPending:An invitation for this person and role is already pending`;
    case 'RoleNotFound':
      return $localize`:@@rosterErrorRoleNotFound:Unknown role`;
    default:
      return code;
  }
}
