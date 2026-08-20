import { computed } from '@angular/core';
import {
  patchState,
  signalStore,
  withComputed,
  withMethods,
  withState,
} from '@ngrx/signals';

import {
  APP_ROLES,
  AppRole,
  AuthSession,
  LoginSession,
} from './auth-session.model';
import { AUTH_SESSION_STORAGE_KEY } from './auth-token';

type AuthSessionState = {
  session: AuthSession | null;
};

const knownRoles = new Set<string>(Object.values(APP_ROLES));

function storage(): Storage | null {
  return typeof sessionStorage === 'undefined' ? null : sessionStorage;
}

function isExpired(session: AuthSession): boolean {
  const expiresAt = Date.parse(session.expiresAtUtc);
  return !Number.isFinite(expiresAt) || expiresAt <= Date.now();
}

function readStoredSession(): AuthSession | null {
  const sessionStorageRef = storage();
  const stored = sessionStorageRef?.getItem(AUTH_SESSION_STORAGE_KEY);

  if (!stored) {
    return null;
  }

  try {
    const session = JSON.parse(stored) as AuthSession;

    if (
      !session.accessToken ||
      !session.expiresAtUtc ||
      !Array.isArray(session.roles) ||
      isExpired(session)
    ) {
      sessionStorageRef?.removeItem(AUTH_SESSION_STORAGE_KEY);
      return null;
    }

    return {
      ...session,
      roles: session.roles.filter((role) => knownRoles.has(role)),
      profileComplete: session.profileComplete ?? null,
    };
  } catch {
    sessionStorageRef?.removeItem(AUTH_SESSION_STORAGE_KEY);
    return null;
  }
}

function persistSession(session: AuthSession | null): void {
  const sessionStorageRef = storage();

  if (!sessionStorageRef) {
    return;
  }

  if (session) {
    sessionStorageRef.setItem(
      AUTH_SESSION_STORAGE_KEY,
      JSON.stringify(session),
    );
  } else {
    sessionStorageRef.removeItem(AUTH_SESSION_STORAGE_KEY);
  }
}

export const AuthSessionStore = signalStore(
  { providedIn: 'root' },
  withState<AuthSessionState>(() => ({
    session: readStoredSession(),
  })),
  withComputed(({ session }) => ({
    accessToken: computed(() => session()?.accessToken ?? null),
    email: computed(() => session()?.email ?? null),
    roles: computed(() => session()?.roles ?? []),
    profileComplete: computed(() => session()?.profileComplete ?? null),
    isAuthenticated: computed(() => {
      const current = session();
      return current !== null && !isExpired(current);
    }),
  })),
  withMethods((store) => ({
    startSession(login: LoginSession, email: string): void {
      const session: AuthSession = {
        accessToken: login.accessToken,
        tokenType: login.tokenType,
        expiresAtUtc: login.expiresAtUtc,
        roles: login.roles.filter((role): role is AppRole =>
          knownRoles.has(role),
        ),
        email: email.trim().toLowerCase(),
        profileComplete: null,
      };

      persistSession(session);
      patchState(store, { session });
    },

    clear(): void {
      persistSession(null);
      patchState(store, { session: null });
    },

    ensureFresh(): boolean {
      const session = store.session();

      if (!session || isExpired(session)) {
        persistSession(null);
        patchState(store, { session: null });
        return false;
      }

      return true;
    },

    hasRole(role: AppRole): boolean {
      return store.session()?.roles.includes(role) ?? false;
    },

    markProfileComplete(isComplete: boolean): void {
      const current = store.session();

      if (!current) {
        return;
      }

      const session = {
        ...current,
        profileComplete: isComplete,
      };

      persistSession(session);
      patchState(store, { session });
    },

    defaultRoute(): string {
      const roles = store.session()?.roles ?? [];

      if (roles.includes(APP_ROLES.superAdmin)) {
        return '/admin';
      }

      if (roles.includes(APP_ROLES.operationsManager)) {
        return '/operations';
      }

      return '/applicant/profile';
    },
  })),
);
