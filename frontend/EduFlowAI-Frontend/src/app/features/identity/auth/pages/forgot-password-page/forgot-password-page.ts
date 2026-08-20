import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthCopy, AuthCopyKey } from '../../auth-copy';
import { AuthApiService } from '../../data-access/auth-api.service';
import { ForgotPasswordResponse } from '../../models/auth.models';
import { errorStatus } from '../../utils/auth-error.util';

@Component({
  selector: 'app-forgot-password-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password-page.html',
  styleUrl: '../../auth-page.scss',
})
export class ForgotPasswordPage {
  private readonly api = inject(AuthApiService);
  private readonly formBuilder = inject(FormBuilder).nonNullable;

  readonly copy = inject(AuthCopy);
  readonly submitting = signal(false);
  readonly result = signal<ForgotPasswordResponse | null>(null);
  readonly submittedEmail = signal('');
  readonly serverError = signal<AuthCopyKey | null>(null);

  readonly form = this.formBuilder.group({
    email: [
      '',
      [Validators.required, Validators.email, Validators.maxLength(256)],
    ],
  });

  submit(): void {
    this.serverError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const email = this.form.controls.email.value.trim().toLowerCase();
    this.submitting.set(true);

    this.api
      .forgotPassword({ email })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (response) => {
          this.submittedEmail.set(email);
          this.result.set(response);
        },
        error: (error: unknown) => {
          this.serverError.set(
            errorStatus(error) === 429 ? 'rateLimited' : 'unexpected',
          );
        },
      });
  }

  sendAnother(): void {
    this.result.set(null);
    this.serverError.set(null);
  }

  resetQueryParams(): { email: string; token: string } | null {
    const token = this.result()?.developmentResetToken;
    return token ? { email: this.submittedEmail(), token } : null;
  }
}
