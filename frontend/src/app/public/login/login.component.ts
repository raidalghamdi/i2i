import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthApiService } from '../../core/auth/auth-api.service';
import { IdentityService } from '../../core/auth/identity.service';
import { TokenStorageService } from '../../core/auth/token-storage.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authApi = inject(AuthApiService);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly identityService = inject(IdentityService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);
    this.submitting.set(true);
    try {
      const { email, password } = this.form.getRawValue();
      const result = await this.authApi.login(email, password);
      this.tokenStorage.set(result);
      await this.identityService.load();
      await this.router.navigateByUrl('/dashboard');
    } catch {
      this.errorMessage.set($localize`:@@loginError:Incorrect email or password.`);
    } finally {
      this.submitting.set(false);
    }
  }
}
