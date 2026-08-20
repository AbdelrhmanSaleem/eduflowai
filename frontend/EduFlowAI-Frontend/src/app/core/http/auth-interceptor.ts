import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { AuthSessionStore } from '../auth/auth-session.store';
import { RuntimeConfig } from '../config/runtime-config';
import { ApiError, isApiError } from '../errors/api-problem';

function isAuthenticationEndpoint(url: string): boolean {
  return /\/api\/auth\/(?:login|register|confirm-email|forgot-password|reset-password)(?:[/?#]|$)/i.test(
    url,
  );
}

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(AuthSessionStore);
  const runtimeConfig = inject(RuntimeConfig);
  const router = inject(Router);

  if (!runtimeConfig.isTrustedApiUrl(request.url)) {
    return next(request);
  }

  const hadSession = session.session() !== null;
  const hasFreshSession = session.ensureFresh();
  const accessToken = hasFreshSession ? session.accessToken() : null;
  const isAuthEndpoint = isAuthenticationEndpoint(request.url);

  const redirectToLogin = (): void => {
    const returnUrl =
      typeof window === 'undefined'
        ? undefined
        : `${window.location.pathname}${window.location.search}`;

    void router.navigate(['/auth/login'], {
      queryParams:
        returnUrl && returnUrl !== '/auth/login'
          ? { returnUrl }
          : undefined,
    });
  };

  if (hadSession && !hasFreshSession && !isAuthEndpoint) {
    redirectToLogin();
  }

  const authorizedRequest = accessToken
    ? request.clone({
        setHeaders: {
          Authorization: `Bearer ${accessToken}`,
        },
      })
    : request;

  return next(authorizedRequest).pipe(
    catchError((error: unknown) => {
      const status =
        isApiError(error)
          ? error.status
          : typeof error === 'object' &&
              error !== null &&
              'status' in error &&
              typeof (error as ApiError).status === 'number'
            ? (error as ApiError).status
            : 0;

      if (
        status === 401 &&
        accessToken &&
        !isAuthEndpoint
      ) {
        session.clear();
        redirectToLogin();
      }

      return throwError(() => error);
    }),
  );
};
