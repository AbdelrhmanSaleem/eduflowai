import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';

import { AppRole } from './auth-session.model';
import { AuthSessionStore } from './auth-session.store';

export function roleGuard(allowedRoles: AppRole | AppRole[]): CanMatchFn {
  return (_route, segments) => {
    const session = inject(AuthSessionStore);
    const router = inject(Router);
    const roles = Array.isArray(allowedRoles)
      ? allowedRoles
      : [allowedRoles];

    if (!session.ensureFresh()) {
      const returnUrl = `/${segments
        .map((segment) => segment.path)
        .join('/')}`;

      return router.createUrlTree(['/auth/login'], {
        queryParams: { returnUrl },
      });
    }

    if (roles.some((role) => session.hasRole(role))) {
      return true;
    }

    return router.parseUrl(session.defaultRoute());
  };
}
