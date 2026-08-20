import { AUTH_ROUTES } from './auth.routes';

describe('AUTH_ROUTES', () => {
  it('keeps account-link routes available while another session exists', () => {
    const confirmRoute = AUTH_ROUTES.find(
      (route) => route.path === 'confirm-email',
    );
    const resetRoute = AUTH_ROUTES.find(
      (route) => route.path === 'reset-password',
    );

    expect(confirmRoute?.canActivate).toBeUndefined();
    expect(resetRoute?.canActivate).toBeUndefined();
  });

  it('keeps guest-only guards on credential entry routes', () => {
    for (const path of ['login', 'register', 'forgot-password']) {
      expect(
        AUTH_ROUTES.find((route) => route.path === path)?.canActivate,
      ).toHaveLength(1);
    }
  });
});
