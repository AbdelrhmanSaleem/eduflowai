import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthCopy, AuthCopyKey } from '../../auth-copy';
import { AuthApiService } from '../../data-access/auth-api.service';
import {
  PreferredLanguage,
  RegisterResponse,
} from '../../models/auth.models';
import {
  errorContains,
  errorStatus,
} from '../../utils/auth-error.util';
import {
  matchingPasswordsValidator,
  strongPasswordValidator,
} from '../../utils/password.validators';

@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register-page.html',
  styleUrl: '../../auth-page.scss',
})
export class RegisterPage {
  private readonly api = inject(AuthApiService);
  private readonly formBuilder = inject(FormBuilder).nonNullable;

  readonly copy = inject(AuthCopy);
  readonly showPassword = signal(false);
  readonly showConfirmation = signal(false);
  readonly submitting = signal(false);
  readonly serverError = signal<AuthCopyKey | null>(null);
  readonly result = signal<RegisterResponse | null>(null);

  readonly form = this.formBuilder.group(
    {
      email: [
        '',
        [Validators.required, Validators.email, Validators.maxLength(256)],
      ],
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

    const value = this.form.getRawValue();
    const request = {
      email: value.email.trim().toLowerCase(),
      password: value.password,
      preferredLanguage: this.copy.locale() as PreferredLanguage,
    };

    this.submitting.set(true);
    this.api
      .register(request)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (response) => this.result.set(response),
        error: (error: unknown) => {
          let key: AuthCopyKey = 'unexpected';

          if (
            errorContains(error, 'duplicate') ||
            errorContains(error, 'already')
          ) {
            key = 'duplicateEmail';
          } else if (errorStatus(error) === 429) {
            key = 'rateLimited';
          } else if (errorStatus(error) === 400) {
            key = 'registrationInvalid';
          }

          this.serverError.set(key);
        },
      });
  }

  confirmationQueryParams(): { email: string; token: string } | null {
    const response = this.result();
    const token = response?.developmentConfirmationToken;

    return response && token ? { email: response.email, token } : null;
  }
}
