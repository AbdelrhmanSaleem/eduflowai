import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthCopy, AuthCopyKey } from '../../auth-copy';
import { AuthApiService } from '../../data-access/auth-api.service';
import { AuthRequestState } from '../../models/auth.models';
import { errorStatus } from '../../utils/auth-error.util';
import {
  matchingPasswordsValidator,
  strongPasswordValidator,
} from '../../utils/password.validators';

@Component({
  selector: 'app-reset-password-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password-page.html',
  styleUrl: '../../auth-page.scss',
})
export class ResetPasswordPage {
  private readonly api = inject(AuthApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  private email =
    this.route.snapshot.queryParamMap.get('email')?.trim() ?? '';
  private token = this.route.snapshot.queryParamMap.get('token') ?? '';

  readonly copy = inject(AuthCopy);
  readonly showPassword = signal(false);
  readonly showConfirmation = signal(false);
  readonly state = signal<AuthRequestState>(
    this.email && this.token ? 'idle' : 'invalid-link',
  );
  readonly serverError = signal<AuthCopyKey | null>(null);

  readonly form = this.formBuilder.group(
    {
      password: [
        '',
        [
          Validators.required,
          Validators.maxLength(100),
          strongPasswordValidator,
        ],
      ],
      confirmPassword: [
        '',
        [Validators.required, Validators.maxLength(100)],
      ],
    },
    { validators: matchingPasswordsValidator },
  );

  passwordMeets(rule: 'length' | 'uppercase' | 'lowercase' | 'digit'): boolean {
    const value = this.form.controls.password.value;

    switch (rule) {
      case 'length':
        return value.length >= 8;
      case 'uppercase':
        return /[A-Z]/.test(value);
      case 'lowercase':
        return /[a-z]/.test(value);
      case 'digit':
        return /\d/.test(value);
    }

    return false;
  }

  submit(): void {
    this.serverError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.state.set('submitting');
    this.api
      .resetPassword({
        email: this.email,
        token: this.token,
        newPassword: this.form.controls.password.value,
      })
      .pipe(
        finalize(() => {
          if (this.state() === 'submitting') {
            this.state.set('idle');
          }
        }),
      )
      .subscribe({
        next: () => {
          this.token = '';
          this.state.set('success');
          this.clearSensitiveQueryParams();
        },
        error: (error: unknown) => {
          if (errorStatus(error) === 429) {
            this.serverError.set('rateLimited');
            this.state.set('idle');
          } else if (errorStatus(error) === 400) {
            this.token = '';
            this.state.set('invalid-link');
            this.clearSensitiveQueryParams();
          } else {
            this.serverError.set('unexpected');
            this.state.set('idle');
          }
        },
      });
  }

  private clearSensitiveQueryParams(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {},
      replaceUrl: true,
    });
  }
}
