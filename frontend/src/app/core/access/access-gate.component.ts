import { Component, inject, signal } from '@angular/core'; // Change 20260726
import { ActivatedRoute, Router } from '@angular/router'; // Change 20260726
import { grantAccess } from './access'; // Change 20260726

/** Password screen shown by {@link accessGateGuard} while the platform is in testing. */ // Change 20260726
@Component({ // Change 20260726
  selector: 'app-access-gate', // Change 20260726
  templateUrl: './access-gate.component.html', // Change 20260726
}) // Change 20260726
export class AccessGateComponent { // Change 20260726
  private readonly router = inject(Router); // Change 20260726
  private readonly route = inject(ActivatedRoute); // Change 20260726

  readonly password = signal(''); // Change 20260726
  readonly incorrect = signal(false); // Change 20260726
  readonly submitting = signal(false); // Change 20260726

  async submit(): Promise<void> { // Change 20260726
    this.incorrect.set(false); // Change 20260726
    this.submitting.set(true); // Change 20260726
    try { // Change 20260726
      if (!(await grantAccess(this.password()))) { // Change 20260726
        this.incorrect.set(true); // Change 20260726
        return; // Change 20260726
      } // Change 20260726
      await this.router.navigateByUrl(this.route.snapshot.queryParamMap.get('next') ?? '/'); // Change 20260726
    } finally { // Change 20260726
      this.submitting.set(false); // Change 20260726
    } // Change 20260726
  } // Change 20260726
} // Change 20260726
