import { Routes } from '@angular/router';

import { guestGuard } from '../../../core/auth/guest-guard';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    title: 'Log in | ITI Admissions',
    loadComponent: () =>
      import('./pages/login-page/login-page').then(
        (component) => component.LoginPage,
      ),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    title: 'Register | ITI Admissions',
    loadComponent: () =>
      import('./pages/register-page/register-page').then(
        (component) => component.RegisterPage,
      ),
  },
  {
    path: 'confirm-email',
    title: 'Confirm email | ITI Admissions',
    loadComponent: () =>
      import('./pages/confirm-email-page/confirm-email-page').then(
        (component) => component.ConfirmEmailPage,
      ),
  },
  {
    path: 'forgot-password',
    canActivate: [guestGuard],
    title: 'Forgot password | ITI Admissions',
    loadComponent: () =>
      import('./pages/forgot-password-page/forgot-password-page').then(
        (component) => component.ForgotPasswordPage,
      ),
  },
  {
    path: 'reset-password',
    title: 'Reset password | ITI Admissions',
    loadComponent: () =>
      import('./pages/reset-password-page/reset-password-page').then(
        (component) => component.ResetPasswordPage,
      ),
  },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
];

export default AUTH_ROUTES;
