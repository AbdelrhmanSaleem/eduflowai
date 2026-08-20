import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthSessionStore } from '../../../../../core/auth/auth-session.store';
import { AuthCopy, AuthCopyKey } from '../../auth-copy';
import { AuthApiService } from '../../data-access/auth-api.service';
import {
  errorContains,
  errorStatus,
  safeReturnUrl,
} from '../../utils/auth-error.util';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login-page.html',
  styleUrl: '../../auth-page.scss',
})
export class LoginPage {
  private readonly api = inject(AuthApiService);
  private readonly authSession = inject(AuthSessionStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder).nonNullable;

  readonly copy = inject(AuthCopy);
  readonly showPassword = signal(false);
  readonly submitting = signal(false);
  readonly serverError = signal<AuthCopyKey | null>(null);

  readonly form = this.formBuilder.group({
    email: [
      '',
      [Validators.required, Validators.email, Validators.maxLength(256)],
    ],
    password: ['', [Validators.required, Validators.maxLength(100)]],
  });

  togglePassword(): void {
    this.showPassword.update((visible) => !visible);
  }

  submit(): void {
    this.serverError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request = {
      email: this.form.controls.email.value.trim().toLowerCase(),
      password: this.form.controls.password.value,
    };

    this.submitting.set(true);
    this.api
      .login(request)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (response) => {
          this.authSession.startSession(response, request.email);

          const returnUrl = safeReturnUrl(
            this.route.snapshot.queryParamMap.get('returnUrl'),
          );

          void this.router.navigateByUrl(
            returnUrl ?? this.authSession.defaultRoute(),
          );
        },
        error: (error: unknown) => {
          const status = errorStatus(error);
          let key: AuthCopyKey = 'unexpected';

          if (status === 401) {
            key = 'invalidCredentials';
          } else if (status === 403) {
            if (
              errorContains(error, 'inactive') ||
              errorContains(error, 'deactivated')
            ) {
              key = 'accountInactive';
            } else if (
              errorContains(error, 'confirm') ||
              errorContains(error, 'confirmation')
            ) {
              key = 'accountUnconfirmed';
            } else {
              key = 'accountUnavailable';
            }
          } else if (status === 423) {
            key = 'accountLocked';
          } else if (status === 429) {
            key = 'rateLimited';
          }

          this.serverError.set(key);
        },
      });
  }
}
