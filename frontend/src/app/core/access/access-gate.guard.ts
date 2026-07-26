import { inject } from '@angular/core'; // Change 20260726
import { CanActivateFn, Router, UrlTree } from '@angular/router'; // Change 20260726
import { ACCESS_QUERY_PARAM, grantAccess, isAccessGranted, revokeAccess } from './access'; // Change 20260726

/** // Change 20260726
 * Holds every route behind the shared testing password. A correct `?access=` parameter // Change 20260726
 * unlocks in place so testing links can be shared; the parameter is then stripped from the // Change 20260726
 * URL so the password does not linger in the address bar or browser history. // Change 20260726
 */ // Change 20260726
export const accessGateGuard: CanActivateFn = async (route, state) => { // Change 20260726
  const router = inject(Router); // Change 20260726
  if (isAccessGranted()) { // Change 20260726
    return true; // Change 20260726
  } // Change 20260726

  const requested = router.parseUrl(state.url); // Change 20260726
  delete requested.queryParams[ACCESS_QUERY_PARAM]; // Change 20260726

  const supplied = route.queryParamMap.get(ACCESS_QUERY_PARAM); // Change 20260726
  if (supplied !== null && (await grantAccess(supplied))) { // Change 20260726
    return requested; // Change 20260726
  } // Change 20260726

  return router.createUrlTree(['/gate'], { queryParams: { next: requested.toString() } }); // Change 20260726
}; // Change 20260726

/** Utility route: drops the stored token and sends the visitor back to the gate. */ // Change 20260726
export const accessLogoutGuard: CanActivateFn = (): UrlTree => { // Change 20260726
  const router = inject(Router); // Change 20260726
  revokeAccess(); // Change 20260726
  return router.createUrlTree(['/gate']); // Change 20260726
}; // Change 20260726
