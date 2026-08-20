import {
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthCopy, AuthCopyKey } from '../../auth-copy';
import { AuthApiService } from '../../data-access/auth-api.service';
import { AuthRequestState } from '../../models/auth.models';
import { errorStatus } from '../../utils/auth-error.util';

@Component({
  selector: 'app-confirm-email-page',
  imports: [RouterLink],
  templateUrl: './confirm-email-page.html',
  styleUrl: '../../auth-page.scss',
})
export class ConfirmEmailPage implements OnInit {
  private readonly api = inject(AuthApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private email = '';
  private token = '';

  readonly copy = inject(AuthCopy);
  readonly state = signal<AuthRequestState>('idle');
  readonly errorKey = signal<AuthCopyKey>('confirmationInvalid');

  ngOnInit(): void {
    this.email = this.route.snapshot.queryParamMap.get('email')?.trim() ?? '';
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    this.confirm();
  }

  retry(): void {
    this.confirm();
  }

  canRetry(): boolean {
    return Boolean(this.email && this.token);
  }

  private confirm(): void {
    if (!this.email || !this.token) {
      this.state.set('invalid-link');
      return;
    }

    this.state.set('submitting');
    this.api
      .confirmEmail({ email: this.email, token: this.token })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.token = '';
          this.state.set('success');
          void this.router.navigate([], {
            relativeTo: this.route,
            queryParams: {},
            replaceUrl: true,
          });
        },
        error: (error: unknown) => {
          const status = errorStatus(error);

          if (status === 429) {
            this.errorKey.set('rateLimited');
            this.state.set('error');
          } else if (status === 400) {
            this.token = '';
            this.errorKey.set('confirmationInvalid');
            this.state.set('invalid-link');
            void this.router.navigate([], {
              relativeTo: this.route,
              queryParams: {},
              replaceUrl: true,
            });
          } else {
            this.errorKey.set('confirmationTechnicalFailure');
            this.state.set('error');
          }
        },
      });
  }
}
