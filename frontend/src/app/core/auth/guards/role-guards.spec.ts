import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { signal } from '@angular/core';
import { IdentityService } from '../identity.service';
import { RoleCodes } from '../role-codes';
import {
  adminOnlyGuard,
  anyAssignedRoleGuard,
  evaluatorAndAboveGuard,
  supervisorOrAdminGuard,
  supervisorOrCommitteeGuard,
} from './role-guards';

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

describe('role guards', () => {
  it('adminOnlyGuard allows admin and denies supervisor', () => {
    configure(RoleCodes.Admin);
    expect(TestBed.runInInjectionContext(() => adminOnlyGuard(DUMMY_ROUTE, DUMMY_STATE))).toBe(true);

    configure(RoleCodes.Supervisor);
    expect(TestBed.runInInjectionContext(() => adminOnlyGuard(DUMMY_ROUTE, DUMMY_STATE))).toBe(REDIRECT_MARKER);
  });

  it('supervisorOrCommitteeGuard allows supervisor, judge, and admin, denies evaluator', () => {
    for (const role of [RoleCodes.Supervisor, RoleCodes.Judge, RoleCodes.Admin]) {
      configure(role);
      expect(TestBed.runInInjectionContext(() => supervisorOrCommitteeGuard(DUMMY_ROUTE, DUMMY_STATE))).toBe(true);
    }

    configure(RoleCodes.Evaluator);
    expect(TestBed.runInInjectionContext(() => supervisorOrCommitteeGuard(DUMMY_ROUTE, DUMMY_STATE))).toBe(REDIRECT_MARKER);
  });

  it('supervisorOrAdminGuard allows supervisor and admin, denies judge', () => {
    for (const role of [RoleCodes.Supervisor, RoleCodes.Admin]) {
      configure(role);
      expect(TestBed.runInInjectionContext(() => supervisorOrAdminGuard(DUMMY_ROUTE, DUMMY_STATE))).toBe(true);
    }

    configure(RoleCodes.Judge);
    expect(TestBed.runInInjectionContext(() => supervisorOrAdminGuard(DUMMY_ROUTE, DUMMY_STATE))).toBe(REDIRECT_MARKER);
  });

  it('evaluatorAndAboveGuard allows evaluator, judge, supervisor, and admin, denies submitter', () => {
    for (const role of [RoleCodes.Evaluator, RoleCodes.Judge, RoleCodes.Supervisor, RoleCodes.Admin]) {
      configure(role);
      expect(TestBed.runInInjectionContext(() => evaluatorAndAboveGuard(DUMMY_ROUTE, DUMMY_STATE))).toBe(true);
    }

    configure(RoleCodes.Submitter);
    expect(TestBed.runInInjectionContext(() => evaluatorAndAboveGuard(DUMMY_ROUTE, DUMMY_STATE))).toBe(REDIRECT_MARKER);
  });

  it('anyAssignedRoleGuard allows all 5 core roles, denies a user with zero roles', () => {
    for (const role of [RoleCodes.Submitter, RoleCodes.Evaluator, RoleCodes.Judge, RoleCodes.Supervisor, RoleCodes.Admin]) {
      configure(role);
      expect(TestBed.runInInjectionContext(() => anyAssignedRoleGuard(DUMMY_ROUTE, DUMMY_STATE))).toBe(true);
    }

    configure(null);
    expect(TestBed.runInInjectionContext(() => anyAssignedRoleGuard(DUMMY_ROUTE, DUMMY_STATE))).toBe(REDIRECT_MARKER);
  });
});
