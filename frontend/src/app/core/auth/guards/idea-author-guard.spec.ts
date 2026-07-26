import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { signal } from '@angular/core';
import { IdentityService } from '../identity.service';
import { RoleCodes } from '../role-codes';
import { ideaAuthorGuard } from './idea-author-guard';

const DUMMY_ROUTE = {} as never;
const DUMMY_STATE = {} as never;
const REDIRECT_MARKER = 'REDIRECT' as never;

function configure(activeRole: string | null): void {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      {
        provide: IdentityService,
        useValue: {
          identity: signal({
            samAccountName: 'x',
            email: null,
            department: null,
            roles: activeRole ? [activeRole] : [],
            activeRole,
          }),
        },
      },
      {
        provide: Router,
        useValue: { createUrlTree: () => REDIRECT_MARKER },
      },
    ],
  });
}

describe('ideaAuthorGuard', () => {
  it('allows submitter and admin', () => {
    for (const role of [RoleCodes.Submitter, RoleCodes.Admin]) {
      configure(role);
      expect(TestBed.runInInjectionContext(() => ideaAuthorGuard(DUMMY_ROUTE, DUMMY_STATE))).toBe(true);
    }
  });

  it('denies evaluator, judge, and supervisor', () => {
    for (const role of [RoleCodes.Evaluator, RoleCodes.Judge, RoleCodes.Supervisor]) {
      configure(role);
      expect(TestBed.runInInjectionContext(() => ideaAuthorGuard(DUMMY_ROUTE, DUMMY_STATE))).toBe(REDIRECT_MARKER);
    }
  });

  it('denies a user with no active role', () => {
    configure(null);
    expect(TestBed.runInInjectionContext(() => ideaAuthorGuard(DUMMY_ROUTE, DUMMY_STATE))).toBe(REDIRECT_MARKER);
  });
});
