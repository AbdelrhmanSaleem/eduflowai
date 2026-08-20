import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { catchError, map, of, tap } from 'rxjs';

import { RuntimeConfig } from '../config/runtime-config';
import { AuthSessionStore } from './auth-session.store';

type ProfileStatusResponse = {
  isComplete: boolean;
};

export const profileCompleteGuard: CanMatchFn = () => {
  const session = inject(AuthSessionStore);
  const http = inject(HttpClient);
  const router = inject(Router);
  const runtimeConfig = inject(RuntimeConfig);
  const knownStatus = session.profileComplete();

  if (knownStatus !== null) {
    return knownStatus
      ? true
      : router.createUrlTree(['/applicant/profile']);
  }

  return http
    .get<ProfileStatusResponse>(`${runtimeConfig.apiBaseUrl}/profile`)
    .pipe(
      tap((profile) => session.markProfileComplete(profile.isComplete)),
      map((profile) =>
        profile.isComplete
          ? true
          : router.createUrlTree(['/applicant/profile']),
      ),
      catchError(() =>
        of(router.createUrlTree(['/applicant/profile'])),
      ),
    );
};
