import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';

import { AuthSessionStore } from './auth-session.store';

export const authGuard: CanMatchFn = (_route, segments) => {
  const session = inject(AuthSessionStore);
  const router = inject(Router);

  if (session.ensureFresh()) {
    return true;
  }

  const returnUrl = `/${segments.map((segment) => segment.path).join('/')}`;

  return router.createUrlTree(['/auth/login'], {
    queryParams: returnUrl === '/' ? undefined : { returnUrl },
  });
};
