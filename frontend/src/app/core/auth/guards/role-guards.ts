import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { IdentityService } from '../identity.service';
import { RoleCodes } from '../role-codes';

function checkRole(allowed: readonly string[]): boolean | UrlTree {
  const identityService = inject(IdentityService);
  const router = inject(Router);
  const activeRole = identityService.identity()?.activeRole;
  if (activeRole && allowed.includes(activeRole)) {
    return true;
  }
  return router.createUrlTree(['/']);
}

export const adminOnlyGuard: CanActivateFn = () => checkRole([RoleCodes.Admin]);

export const supervisorOrCommitteeGuard: CanActivateFn = () =>
  checkRole([RoleCodes.Supervisor, RoleCodes.Judge, RoleCodes.Admin]);

export const supervisorOrAdminGuard: CanActivateFn = () =>
  checkRole([RoleCodes.Supervisor, RoleCodes.Admin]);

export const evaluatorAndAboveGuard: CanActivateFn = () =>
  checkRole([RoleCodes.Evaluator, RoleCodes.Judge, RoleCodes.Supervisor, RoleCodes.Admin]);

export const anyAssignedRoleGuard: CanActivateFn = () =>
  checkRole([RoleCodes.Submitter, RoleCodes.Evaluator, RoleCodes.Judge, RoleCodes.Supervisor, RoleCodes.Admin]);
