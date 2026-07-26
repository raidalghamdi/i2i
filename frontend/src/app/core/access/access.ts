/** // Change 20260726
 * Shared-password gate used only while the platform is in testing. // Change 20260726
 * // Change 20260726
 * The plain password is deliberately absent from this file (and therefore from the built // Change 20260726
 * bundle): we ship the SHA-256 digest and hash the visitor's input before comparing. That // Change 20260726
 * keeps the password out of a DevTools text search, but it is obfuscation, not security — // Change 20260726
 * anything genuinely protected is gated server-side by AD/Negotiate. // Change 20260726
 */ // Change 20260726

export const ACCESS_TOKEN_KEY = 'i2i_access_token'; // Change 20260726

/** SHA-256 of the shared testing password. */ // Change 20260726
export const ACCESS_PASSWORD_HASH = // Change 20260726
  '79a06890a399ce593d94d084ca52c331c15e556244343f6602e1bd93fee62517'; // Change 20260726

/** Query parameter that auto-submits the gate, for shareable testing links. */ // Change 20260726
export const ACCESS_QUERY_PARAM = 'access'; // Change 20260726

export async function sha256Hex(value: string): Promise<string> { // Change 20260726
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(value)); // Change 20260726
  return Array.from(new Uint8Array(digest)) // Change 20260726
    .map((byte) => byte.toString(16).padStart(2, '0')) // Change 20260726
    .join(''); // Change 20260726
} // Change 20260726

export function isAccessGranted(): boolean { // Change 20260726
  return localStorage.getItem(ACCESS_TOKEN_KEY) === ACCESS_PASSWORD_HASH; // Change 20260726
} // Change 20260726

/** Stores the token when the password is right; reports whether it was. */ // Change 20260726
export async function grantAccess(password: string): Promise<boolean> { // Change 20260726
  const hash = await sha256Hex(password); // Change 20260726
  if (hash !== ACCESS_PASSWORD_HASH) { // Change 20260726
    return false; // Change 20260726
  } // Change 20260726
  localStorage.setItem(ACCESS_TOKEN_KEY, hash); // Change 20260726
  return true; // Change 20260726
} // Change 20260726

export function revokeAccess(): void { // Change 20260726
  localStorage.removeItem(ACCESS_TOKEN_KEY); // Change 20260726
} // Change 20260726
