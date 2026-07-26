import { translateRosterErrorCode } from './roster-error-messages';

describe('translateRosterErrorCode', () => {
  it('translates AdUserNotFound to a localized, human-readable message', () => {
    expect(translateRosterErrorCode('AdUserNotFound')).toBe('AD account not found');
  });

  it('translates AlreadyApplied to a localized, human-readable message', () => {
    expect(translateRosterErrorCode('AlreadyApplied')).toBe('This person already has this role');
  });

  it('translates AlreadyPending to a localized, human-readable message', () => {
    expect(translateRosterErrorCode('AlreadyPending')).toBe(
      'An invitation for this person and role is already pending',
    );
  });

  it('translates RoleNotFound to a localized, human-readable message', () => {
    expect(translateRosterErrorCode('RoleNotFound')).toBe('Unknown role');
  });

  it('falls back to returning an unrecognized raw code unchanged', () => {
    expect(translateRosterErrorCode('SomeFutureStatus')).toBe('SomeFutureStatus');
  });
});
