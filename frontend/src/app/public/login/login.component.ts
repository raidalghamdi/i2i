import { Component, inject, signal } from '@angular/core'; // Change 20260726
import { Router } from '@angular/router'; // Change 20260726
import { IdentityService } from '../../core/auth/identity.service'; // Change 20260726

/** // Change 20260726
 * Sign-in is delegated to Active Directory (Negotiate): there is no password to collect here. // Change 20260726
 * Requesting a protected endpoint triggers the browser's own AD handshake, after which the // Change 20260726
 * identity is available and we can move on to the workboard. // Change 20260726
 */ // Change 20260726
@Component({ // Change 20260726
  selector: 'app-login', // Change 20260726
  templateUrl: './login.component.html', // Change 20260726
}) // Change 20260726
export class LoginComponent { // Change 20260726
  private readonly identityService = inject(IdentityService); // Change 20260726
  private readonly router = inject(Router); // Change 20260726

  readonly submitting = signal(false); // Change 20260726
  readonly errorMessage = signal<string | null>(null); // Change 20260726

  async signInWithGac(): Promise<void> { // Change 20260726
    this.errorMessage.set(null); // Change 20260726
    this.submitting.set(true); // Change 20260726
    try { // Change 20260726
      await this.identityService.load(); // Change 20260726
      if (this.identityService.loadFailed()) { // Change 20260726
        this.errorMessage.set($localize`:@@authSignInFailed:Could not sign you in with your GAC account. Contact your administrator if this continues.`); // Change 20260726
        return; // Change 20260726
      } // Change 20260726
      await this.router.navigateByUrl('/dashboard'); // Change 20260726
    } finally { // Change 20260726
      this.submitting.set(false); // Change 20260726
    } // Change 20260726
  } // Change 20260726
} // Change 20260726
