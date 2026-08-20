import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthSessionStore } from './auth-session.store';

export const guestGuard: CanActivateFn = () => {
  const session = inject(AuthSessionStore);
  const router = inject(Router);

  return session.ensureFresh()
    ? router.parseUrl(session.defaultRoute())
    : true;
};
