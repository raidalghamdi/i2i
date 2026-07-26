import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { IdentityService } from '../identity.service';
import { RoleCodes } from '../role-codes';

const IDEA_AUTHOR_ROLES: readonly string[] = [RoleCodes.Submitter, RoleCodes.Admin];

/**
 * Idea authoring (create/edit) is restricted to the submitter and admin roles.
 * Evaluator/judge/supervisor (and mentor/facilitator/expert) may be assigned
 * reviewer duties on ideas but must never be able to author one.
 */
export const ideaAuthorGuard: CanActivateFn = (): boolean | UrlTree => {
  const identityService = inject(IdentityService);
  const router = inject(Router);
  const activeRole = identityService.identity()?.activeRole;
  if (activeRole && IDEA_AUTHOR_ROLES.includes(activeRole)) {
    return true;
  }
  return router.createUrlTree(['/']);
};
