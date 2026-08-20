import { TestBed } from '@angular/core/testing';

import { AUTH_SESSION_STORAGE_KEY } from './auth-token';
import { AuthSessionStore } from './auth-session.store';

describe('AuthSessionStore', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({});
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('persists a valid login and discards unknown roles', () => {
    const store = TestBed.inject(AuthSessionStore);

    store.startSession(
      {
        accessToken: 'access-token',
        tokenType: 'Bearer',
        expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
        roles: ['Applicant', 'UnknownRole'],
      },
      ' Applicant@Example.com ',
    );

    expect(store.isAuthenticated()).toBe(true);
    expect(store.roles()).toEqual(['Applicant']);
    expect(store.email()).toBe('applicant@example.com');
    expect(sessionStorage.getItem(AUTH_SESSION_STORAGE_KEY)).toContain(
      'access-token',
    );
  });

  it('restores a valid session after the store is recreated', () => {
    sessionStorage.setItem(
      AUTH_SESSION_STORAGE_KEY,
      JSON.stringify({
        accessToken: 'restored-token',
        tokenType: 'Bearer',
        expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
        roles: ['OperationsManager'],
        email: 'manager@example.com',
        profileComplete: null,
      }),
    );

    const store = TestBed.inject(AuthSessionStore);

    expect(store.isAuthenticated()).toBe(true);
    expect(store.defaultRoute()).toBe('/operations');
  });

  it('rejects and removes an expired stored session', () => {
    sessionStorage.setItem(
      AUTH_SESSION_STORAGE_KEY,
      JSON.stringify({
        accessToken: 'expired-token',
        tokenType: 'Bearer',
        expiresAtUtc: new Date(Date.now() - 60_000).toISOString(),
        roles: ['Applicant'],
        email: 'applicant@example.com',
        profileComplete: true,
      }),
    );

    const store = TestBed.inject(AuthSessionStore);

    expect(store.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem(AUTH_SESSION_STORAGE_KEY)).toBeNull();
  });

  it('updates profile completeness and clears the session on logout', () => {
    const store = TestBed.inject(AuthSessionStore);
    store.startSession(
      {
        accessToken: 'access-token',
        tokenType: 'Bearer',
        expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
        roles: ['Applicant'],
      },
      'applicant@example.com',
    );

    store.markProfileComplete(true);
    expect(store.profileComplete()).toBe(true);

    store.clear();
    expect(store.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem(AUTH_SESSION_STORAGE_KEY)).toBeNull();
  });
});
